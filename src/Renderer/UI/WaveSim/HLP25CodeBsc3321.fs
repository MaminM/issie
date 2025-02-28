module HLP25CodeBsc3321

open Hlp25Types
open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open WaveSimStyle
open WaveSimHelpers
open SimGraphTypes
open SimTypes
open DiagramStyle
open JSHelpers
open NumberHelpers
open ModelType
open CommonTypes
open WaveSimSelect
open MemoryEditorView
open PopupHelpers
open UIPopups
open Notifications
open Sheet.SheetInterface
open DrawModelType
open FilesIO
open CatalogueView
open TopMenuView
open MenuHelpers

//------------------------------------- Part B ---------------------------------------------------//
//----------------------------- Sample Code for HLP25 --------------------------------------------//
//----------------------------- use these to get started -----------------------------------------//
//-------------------- Modify the signatures as you see fit for your implementation---------------//
//------------------------------------------------------------------------------------------------//






//------------------------------------- My attempt at implementing the makeWaveDisplayTree function---------------------------------------------------//



/// Differentiator between the different node types. 
type WTType =
    | SheetNode
    | ComponentNode
    | LeafNode

/// Node "variable" which lets us keep track of name, type and any waves associated with the current level of hierarchy
type WTNode = {
    Title: string
    Kind: WTNodeKind
    Waves: Wave list
}

/// Internal nodes on the tree with reference to child nodes
type WaveTreeNode = {
    WTNode: WTNode
    Children: WaveTreeNode list
}


/// "Master" return tree which is returned and passed as parameter to the eventual ImplementWaveDisplayTree
type WaveDisplayTree = WaveTreeNode list


/// Filter the waves based on the entered search string(s)
let gatherFilteredWaves (wsModel: WaveSimModel) =
    let okWaves, okSelectedWaves = ensureWaveConsistency wsModel
    let st = wsModel.SearchString.ToUpper()
    applyFiltering wsModel st okWaves okSelectedWaves

/// Group the waves by subsheet
let groupBySubSheet (waves: Wave list) =
    waves
    |> List.groupBy (fun w ->
        match w.SubSheet with
        | [] -> "Top-Level"
        | path -> String.concat "." path
    )

/// Constructor for making a leaf node which is the lowest level of the tree
let makeLeafNode (w: Wave) : WaveTreeNode =
    {
        WTNode = {
            Title = w.ViewerDisplayName
            Kind = LeafNode
            Waves = [w]
        }
        Children = []
    }

/// Constructor for a component node which is an intermediate node of the tree with leaf nodes as children.
let makeComponentNode (compName: ComponentGroup) (wavesInComp: Wave list) : WaveTreeNode =
    let leafNodes =
        wavesInComp
        |> List.map makeLeafNode

    {
        WTNode = {
            Title = string compName
            Kind = ComponentNode
            Waves = []
        }
        Children = leafNodes
    }

/// Constructor for sheet nodes which are the master node(s) of the tree which has component nodes as its children.
let makeSheetNode (sheetName: string) (waves: Wave list) : WaveTreeNode =
    let fs = Simulator.getFastSim()

    let groupedByComp =
        waves
        |> List.groupBy (fun w -> getCompGroup fs w)

    let componentNodes =
        groupedByComp
        |> List.map (fun (grp, wavesInComp) ->
            makeComponentNode grp wavesInComp
        )

    {
        WTNode = {
            Title = sheetName
            Kind = SheetNode
            Waves = []
        }
        Children = componentNodes
    }

/// Top level function to make the Tree by first grouping by subsheet and then calling the subsheet constructor for each grouping->calls the component grouping
/// constuctors->calls the leaf nodes constructors.
let makeWaveDisplayTree (wsModel: WaveSimModel) : WaveDisplayTree =
    let waves = gatherFilteredWaves wsModel
    let grouped = groupBySubSheet waves

    grouped
    |> List.map (fun (sheetName, wavesInSheet) ->
        makeSheetNode sheetName wavesInSheet
    )



//------------------------------------- My attempt at implementing the selectWavesHlp25 function---------------------------------------------------//


/// The functions below all work together to implement the selectWavesHlp25 function.
/// Displays a react element that allows the user to select waves for display in the waveform viewer.
/// Implements just simple flat sheet with grouping by component, giving a clean user interface.
/// I have had to import some modules from WaveSimSelect as there is no forward referencing.

///This is a type which seeks to provide a helpful output selection from the filtering that can be used to displau waves.
type WaveSelectionOutput = {
    WaveList : list<Wave>
    ShowDetails : bool
}

/// Filters the waves based on the entry in the search text. "*" should display all possible selected waves and otherwise it should match based on the "searchText"
let filterSelectedWaves 
    (ws: WaveSimModel)
    (searchText: string)
    (okWaves: list<Wave>)
    (okSelectedWaves: list<WaveIndexT>)
    : list<Wave> =
    
    match searchText with
    | "*" ->
        okSelectedWaves
        |> List.map (fun wi -> ws.AllWaves.[wi])
    | _ ->
        okWaves
        |> List.filter (fun x -> 
            x.ViewerDisplayName.ToUpper().Contains(searchText)
        )
         
 /// function copied from the initial waveSimSelect file.
let ensureWaveConsistency (ws:WaveSimModel) =
        let fs = Simulator.getFastSim()
        let okWaves =
            Map.values ws.AllWaves
            |> Seq.toList
            |> List.filter (fun wave -> Map.containsKey wave.WaveId.Id fs.WaveComps )
        if okWaves.Length <> ws.AllWaves.Count then
            printfn $"EnsureWaveConsistency: waves,Length={okWaves.Length}, ws.Allwaves.Count={ws.AllWaves.Count}"
        let okSelectedWaves =
            ws.SelectedWaves
            |> List.filter (fun selW -> Map.containsKey selW ws.AllWaves)
        if okSelectedWaves.Length <> ws.SelectedWaves.Length then
            printfn $"ok selected waves length = {okSelectedWaves.Length} <> selectedwaves length = {ws.SelectedWaves.Length}"
        okWaves, okSelectedWaves


/// Top level function that controls filtering of which waves to select and passing it onto the rendering stage. Abstracts the filtering and rendering!
let selectWavesHlp25 (ws: WaveSimModel) (dispatch: Msg -> unit) : WaveSelectionOutput =
    if not ws.WaveModalActive then 
        { WaveList = []; ShowDetails = false }
    else
        let okWaves, okSelectedWaves = ensureWaveConsistency ws
        let searchText = ws.SearchString.ToUpper()
        let wavesToDisplay = filterSelectedWaves ws searchText okWaves okSelectedWaves
        let showDetails = 
            ((wavesToDisplay.Length < 10) || searchText.Length > 0)
            && searchText <> "-"
        { WaveList = wavesToDisplay
          ShowDetails = showDetails }

/// Function that handles creation of a single row which contains waves that have been grouped by component per subsheet. This is a UI function that handles UI rendering. 
let makeFlatGroupRow
    (showDetails: bool)
    (ws: WaveSimModel)
    (dispatch: Msg -> Unit)
    (subSheet: string list)
    (grp: ComponentGroup)
    (wavesInGroup: Wave list)
    : ReactElement =
    
    let cBox = GroupItem (grp, subSheet)
    let summaryReact = summaryName ws cBox subSheet wavesInGroup
    let rowItems =
        wavesInGroup
        |> List.map (fun wave ->
            tr [] [
                td [] [ str wave.ViewerDisplayName ]
                td [] [
                    input [
                            Type "Checkbox"
                            OnChange(fun _ -> toggleWaveSelection wave.WaveId ws dispatch )
                            Checked <| isWaveSelected wave.WaveId ws 
                    ] 
                ]
            ]
        )
    makeSelectionGroup showDetails ws dispatch summaryReact rowItems cBox wavesInGroup

/// UI function that is responsible for grouping waves by subsheet->then grouping waves based on component in each subsheet. Calls subfunction to handle rendering of the table rows themselves.
let makeFlatList 
    (ws: WaveSimModel)
    (dispatch: Msg -> Unit)
    (subSheet: string list)
    (waves: Wave list)
    (showDetails: bool)
    : ReactElement =

    let fs = Simulator.getFastSim()

    // 1) Group waves first by their subSheet (Datapath, ControlPath, etc.)
    let groupedBySubSheet =
        waves
        |> List.groupBy (fun w -> 
            match w.SubSheet with
            | [] -> "Top-Level" // Default if no subsheet
            | sheetPath -> String.concat "." sheetPath // Create readable name
        )

    let subSheetRows =
        groupedBySubSheet
        |> List.map (fun (subSheetName, wavesInSubSheet) ->
            let componentGroups =
                wavesInSubSheet
                |> List.groupBy (fun wave -> getCompGroup fs wave)

            let groupRows =
                componentGroups
                |> List.map (fun (grp, groupWaves) ->
                    makeFlatGroupRow showDetails ws dispatch subSheet grp groupWaves
                )

            // Wrap all component groups for this subSheet in a collapsible section
            makeSelectionGroup showDetails ws dispatch (str subSheetName) groupRows (SheetItem [subSheetName]) wavesInSubSheet
        )

    // 3) Wrap everything in a table
    Table.table [
        Table.IsBordered
        Table.IsFullWidth
        Table.Props [ Style [ BorderWidth 0 ] ]
    ] [
        tbody [] subSheetRows
    ]


///Top level UI function that calls the makeFlatList function. The output of HlpSelectWaves25 (target function for assignment) is what is passed as parameter into this function to handle all the rendering. 
let renderwaves (ws: WaveSimModel) (dispatch: Msg -> unit) (waveselect: WaveSelectionOutput) : ReactElement =
    let showDetails = waveselect.ShowDetails
    let wavelist = waveselect.WaveList
    // Use the new flat approach
    makeFlatList ws dispatch [] wavelist showDetails





