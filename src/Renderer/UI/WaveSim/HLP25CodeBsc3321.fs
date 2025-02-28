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

/// 1. An input box for a string that can be used to seach wave names in the Waveform Selector.
/// Any substring of a wave name of form 'sheet.CompName.PortName' should be matched.
/// 2. A search box for parts of sheet names
/// 3. A search box for component names.
/// 4. A search box for port names
/// 5. (optional) a search box for component type
/// Overall search is AND of all five searches.
/// The search boxes should be able to filter a list of wave names in the Waveform Selector.
/// In addition the search boxes must change a breadcrumb display so that the user can see coloured the
/// sheets in which matches are found.
/// The Sheet search box 2.  has additional functionality to allow the user to navigate to the design visually.
/// When a breadcrumb is clicked the corresponding sheet name is displayed in the sheet box and displayed
/// ports and components are restricted to those in the sheet.
/// Box 1 has additional functionality: when it is changed the component and port boxes are emptied. (good?)
/// 



/// Function to configure how filtered components, ports, etc are displayed in the Waveform Selector.
/// This abstracts out decisions about how to display the Waveform Selector from its actual implementation.
let makeWaveDisplayTree (wsModel: WaveSimModel) : WaveDisplayTree =
    // Not required for MVP; but useful for a full implementation.
    // This function could be written as individual HLP25 code.
    //
    // The wave selector display is arranged as a tree of sheets, components and ports.
    // Nodes in the tree correspond to sets of items that are hidden and optionally displayed
    // The actual display is currently implemented by the recursive function makeSheetTree
    // this both doe sthe implementation and determines the recursive tree structure.
    // 
    // This function abstracts out the tree structure from the implementation.
    // It could be used to implement an optimal tree structure for a given set of
    // filtered waves.
    failwithf "Not implemented yet"

/// Converts a tree of wave display nodes into a react element that can be displayed in the Waveform Selector.
/// The output display uses check boxes and clickables to display or hide nodes, and select/deselect waves. 
let implementWaveSelector (wsModel: WaveSimModel) (dispatch: Msg -> unit) (wTree: WaveDisplayTree): ReactElement =
    // This function implements the display of the waveform selection table
    // as a set of rows that can be hidden or displayed.
    // The structure and order of the rows is determined by the tree structure.
    // Details to display in each row are determined by the node content.
    // Additional details can be looked up from wsModel if necessary.
    // This is not required for MVP, but useful for a full implementation.
    // It could be implemented as HLP25 individual code.
    failwithf "Not implemented yet"






/// The functions below all work together to implement the selectWavesHlp25 function.
/// Displays a react element that allows the user to select waves for display in the waveform viewer.
/// Implements just simple flat sheet with grouping by component, giving a clean user interface.
/// I have had to import some modules from WaveSimSelect as there is no forward referencing.
/// 

///This is a type which seeks to provide a helpful output selection from the filtering that can be used to displau waves.
type WaveSelectionOutput = {
    WaveList : list<Wave>
    ShowDetails : bool
}

/// Filters the waves based on the entry in the search text. "*" should display all possible selected waves and otherwise it should match based on the "searchText"
let applyFiltering 
    (ws: WaveSimModel)
    (searchText: string)
    (okWaves: list<Wave>)
    (okSelectedWaves: list<WaveIndexT>)
    : list<Wave> =
    
    match searchText with
    | "*" ->
        okSelectedWaves
        |> List.map (fun wi -> ws.AllWaves[wi])
    | _ ->
        okWaves
        |> List.filter (fun x -> x.ViewerDisplayName.ToUpper().Contains(searchText))

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
        let wavesToDisplay = applyFiltering ws searchText okWaves okSelectedWaves
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



let selectWavesModalHlp25 (wsModel: WaveSimModel) (dispatch: Msg -> unit) : ReactElement =
    // See WaveSimSelect.selectWavesModal for the existing Waveform Selector top level view
    // This contains a search box to filter waves, and a wave selection box to select/deselect
    // the (filtered) waves for display.
    // Both will be changed in part B
    //
    // The wave selection box will be replaced by a breadcrumb display,
    // and a new wave selection box, side-by-side
    //
    // The current wave selection rows are displayed in a table by the recursive function makeSheetRow
    // This makes, hierarchically, rows for sheets and its components and ports
    // Subsheets are displayed as a single row with a button to open the subsheet
    //
    // The new wave selection rows should not use this sheet hierarchy (the breadcrums deal with that).
    // For an MVP they could be displayed as a flat list of components names, which open when clicked to show ports.
    // This is a simplification of the current display, and implemented in the function makeComponentRow.
    //
    // For a full implementation:
    // The display should depend on the number of (filtered) waves, fewer waves should show more detail.
    // Each row should contain the component sheet name.
    // Whether ports are hidden or not should depend on the number of waves.
    // Each row should (maybe) include component group, with (maybe) the list ordered by class and then component name
    //        See makeComponentGroup for how components are grouped in current display.
    // The display should be adjusted so that the user can quickly select any number of waves.
    // It should also be possible to select all waves in a sheet.
    // Note that each signal can be selected in multiple places, from its driving port, and its receiving port(s).
    // Although these are separate waves only one wave from each signal will be allowed in the waveform viewer.
    // Duplicates are filtered out: 
    failwithf "Not implemented yet"





/// Represents the user's current wave selection plus a flag controlling detail display.
type WaveSelectionOutput = {
    WaveList : list<Wave>
    ShowDetails : bool
}

/// Ensures that all waves in ws.AllWaves are consistent with the simulator model,
/// and returns only those waves that actually exist in fs.WaveComps.
/// Also filters ws.SelectedWaves to exclude any that are no longer valid.
let ensureWaveConsistency (ws: WaveSimModel) =
    let fs = Simulator.getFastSim()

    // Filter out any waves that no longer exist in fs.WaveComps
    let validWaves =
        ws.AllWaves
        |> Map.values         // Convert from map to seq of wave
        |> Seq.toList
        |> List.filter (fun wave -> Map.containsKey wave.WaveId.Id fs.WaveComps)

    if validWaves.Length <> ws.AllWaves.Count then
        printfn "EnsureWaveConsistency: validWaves.Length=%d, ws.AllWaves.Count=%d"
                validWaves.Length ws.AllWaves.Count

    // Filter selected waves so we only keep valid ones
    let validSelectedWaves =
        ws.SelectedWaves
        |> List.filter (fun selW -> Map.containsKey selW ws.AllWaves)

    if validSelectedWaves.Length <> ws.SelectedWaves.Length then
        printfn "Some selected waves were invalid: %d vs. %d"
                validSelectedWaves.Length ws.SelectedWaves.Length

    validWaves, validSelectedWaves


let applyFiltering
    (ws: WaveSimModel)
    (searchText: string)
    (okWaves: list<Wave>)
    (okSelectedWaves: list<WaveIndexT>)
    : list<Wave> =

    match searchText with
    | "*" ->
        okSelectedWaves
        |> List.map (fun waveId -> ws.AllWaves[waveId])
    | _ ->
        okWaves
        |> List.filter (fun wave ->
            wave.ViewerDisplayName.ToUpper().Contains(searchText)
        )

/// Produces a WaveSelectionOutput with a list of filtered waves plus a ShowDetails boolean.
/// If ws.WaveModalActive is false, returns an empty selection.
/// Otherwise, ensures wave consistency, applies filtering, and sets showDetails.
let selectWavesHlp25 (ws: WaveSimModel) (dispatch: Msg -> unit) : WaveSelectionOutput =
    if not ws.WaveModalActive then
        { WaveList = []
          ShowDetails = false }
    else
        let allWaves, selectedWaveIds = ensureWaveConsistency ws
        let searchText = ws.SearchString.ToUpper()

        // Filter waves according to search
        let wavesToDisplay =
            applyFiltering ws searchText allWaves selectedWaveIds

        // Decide if we show details (few waves or user typed something)
        let showDetails =
            (wavesToDisplay.Length < 10 || searchText.Length > 0)
            && searchText <> "-"

        { WaveList = wavesToDisplay
          ShowDetails = showDetails }

/// Builds a single group row for a set of waves that share a common component group.
/// Each wave row has a checkbox plus the wave's ViewerDisplayName.
/// The entire group is wrapped in a makeSelectionGroup call for expand/collapse.
let makeFlatGroupRow
    (showDetails: bool)
    (ws: WaveSimModel)
    (dispatch: Msg -> Unit)
    (subSheet: string list)       // Typically unused or empty, can remove if not needed
    (componentGroup: ComponentGroup)
    (wavesInGroup: Wave list)
    : ReactElement =

    // A checkbox style or grouping item for display.
    let cBox = GroupItem (componentGroup, subSheet)

    // The summary label for this group row
    let summaryElement = summaryName ws cBox subSheet wavesInGroup

    // Build a row (tr) for each wave in the group
    let rowItems =
        wavesInGroup
        |> List.map (fun wave ->
            tr [] [
                // Wave name
                td [] [ str wave.ViewerDisplayName ]
                // Individual wave checkbox
                td [] [
                    input [
                        Type "Checkbox"
                        // Possibly calls user-defined toggleWaveSelection / isWaveSelected:
                        OnChange (fun _ -> toggleWaveSelection wave.WaveId ws dispatch)
                        Checked (isWaveSelected wave.WaveId ws)
                    ] []
                ]
            ]
        )

    // Wrap the group in a selection group that can expand or collapse
    makeSelectionGroup
        showDetails
        ws
        dispatch
        summaryElement
        rowItems
        cBox
        wavesInGroup

/// Renders a flat list of waves, grouped first by subSheet name, then by component group.
/// Each subSheet is wrapped in a collapsible selection group, containing component groups inside.
let makeFlatList
    (ws: WaveSimModel)
    (dispatch: Msg -> Unit)
    (waves: Wave list)
    (showDetails: bool)
    : ReactElement =

    let fs = Simulator.getFastSim()

    // 1) Group by subSheet name
    let groupedBySubSheet =
        waves
        |> List.groupBy (fun w ->
            match w.SubSheet with
            | [] -> "Top-Level"
            | path -> String.concat "." path
        )

    /// Local helper to handle grouping waves by component within one subSheet,
    /// then wrapping them in a selection group for that subSheet.
    let buildSubSheetRow subSheetName (wavesInSubSheet: Wave list) =
        // Group by component
        let componentGroups =
            wavesInSubSheet
            |> List.groupBy (fun w -> getCompGroup fs w)

        // For each component group, build a group row
        let groupRows =
            componentGroups
            |> List.map (fun (grp, groupWaves) ->
                makeFlatGroupRow showDetails ws dispatch [] grp groupWaves
            )

        // Wrap all component groups for this subSheet in one collapsible "sheet" group
        makeSelectionGroup
            showDetails
            ws
            dispatch
            (str subSheetName)
            groupRows
            (SheetItem [subSheetName])
            wavesInSubSheet

    // 2) Build a row for each subSheet
    let subSheetRows =
        groupedBySubSheet
        |> List.map (fun (subSheetName, wavesInSubSheet) ->
            buildSubSheetRow subSheetName wavesInSubSheet
        )

    // 3) Render everything in a table
    Table.table [
        Table.IsBordered
        Table.IsFullWidth
        Table.Props [ Style [ BorderWidth 0 ] ]
    ] [
        tbody [] subSheetRows
    ]

/// Top-level render function that uses makeFlatList to display the final wave selection UI.
/// Reads the WaveSelectionOutput, then calls makeFlatList with the final wave list.
let renderwaves
    (ws: WaveSimModel)
    (dispatch: Msg -> unit)
    (waveSelection: WaveSelectionOutput)
    : ReactElement =

    let showDetails = waveSelection.ShowDetails
    let waveList    = waveSelection.WaveList

    makeFlatList ws dispatch waveList showDetails
