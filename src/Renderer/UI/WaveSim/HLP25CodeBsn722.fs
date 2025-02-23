module HLP25CodeBsn722

open Hlp25Types
open Fable.React
open Fable.React.Props

open JSHelpers
open NumberHelpers
open ModelType
open CommonTypes
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
//////Added by me//////
open Fulma
open Fulma.Extensions.Wikiki
open MiscMenuView
open Constants
open Browser.Types

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
let searchBoxes (wsModel: WaveSimModel) (dispatch: Msg -> unit) : ReactElement =
    // See MiscMenuView for Breadcrumb generation functions
    // see WaveSelectView for the existing Waveform Selector search box
    // Use the existing Waveform Selector search box as a template for the new search boxes.
    failwithf "Not implemented yet"

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

/// Displays a breadcrumb display of the simulation design sheet hierarchy with
/// coloured sheets indicating where the search string is found. Possibly the number of
/// matches in each sheet is displayed.


/////////////////////////////// Helper Functions //////////////////////////////////////

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

////////////////////////////////////////////////////////////////////////////////////////
let waveSelectBreadcrumbs (wsModel: WaveSimModel) (dispatch: Msg -> unit) (model: Model): ReactElement =
    // See MiscMenuView for Breadcrumb generation functions
    // see WaveSelectView for the existing Waveform Selector search box
    // Use the existing Waveform Selector search box as a template for the new search boxes.
    match model.CurrentProj with
    | None -> div [] [str "No project open"]
    | Some project ->
        let updatedProject = ModelHelpers.getUpdatedLoadedComponents project model
        let updatedModel = {model with CurrentProj = Some updatedProject}
        let okWaves, okSelectedWaves = ensureWaveConsistency wsModel
        let searchText = wsModel.SearchString
        let filteredWaves = 
            match searchText with
            | "" | "-" -> okWaves
            | "*" -> okSelectedWaves |> List.map (fun wi -> wsModel.AllWaves[wi])
            | _ -> List.filter (fun x -> x.ViewerDisplayName.ToUpper().Contains(searchText)) okWaves

        let filteredWaveNames = filteredWaves |> List.map (fun wave -> wave.ViewerDisplayName)

        let sheetNames = 
            filteredWaveNames 
            |> List.collect (fun name -> name.Split('.') |> Array.map (fun s -> s.ToLowerInvariant()) |> Array.toList) 
        
        let sheetCounts = sheetNames |> List.groupBy id |> List.map (fun (name, waves) -> name, waves.Length)  // Hashamp of sheet name to number of waves in that sheet

        let sheetColor (sheet:SheetTree) =
                match List.contains sheet.SheetName sheetNames with
                | false -> IColor.IsCustomColor "darkslategrey"
                | true -> IColor.IsCustomColor "pink"

        let sheetMatches (sheet: SheetTree) =
            match List.tryFind (fun (name, _) -> name = sheet.SheetName) sheetCounts with
            | Some (_, count) -> count
            | None -> 0

        let breadcrumbConfig =  {
            MiscMenuView.Constants.defaultConfig with
                ColorFun = sheetColor
                NoWaves = sheetMatches
            }

        let breadcrumbs = [
                div [Style [TextAlign TextAlignOptions.Center; FontSize "15px"]] [str "Sheets with Design Hierarchy"]
                MiscMenuView.hierarchyBreadcrumbs breadcrumbConfig dispatch updatedModel
                ]

        div [] (breadcrumbs)




/// Displays a react element that allows the user to select waves for display in the waveform viewer.
let selectWavesHlp25 (wsModel: WaveSimModel) (dispatch: Msg -> unit) : ReactElement =
    // see WaveSimSelect.selectWaves for the existing display of waveforms to select
    // For MVP this could be a cut-down version of that function that displays a flat list of components
    // and hidable ports, with a checkbox to select each one.
    // For a full implementation: this function could be abstracted as the two functions above:
    //    makeWaveDisplayTree: that determines the tree structure of the display
    //    implementWaveSelector: that displays the given tree structure
    failwithf "Not implemented yet"


////////////////////// HELPER FUNCTIONS  //////////////////////
let searchBox
    (placeholder : string)
    (wsModel : WaveSimModel)
    (dispatch : Msg -> unit)
    : ReactElement =
    
    Control.p [ Control.IsExpanded ] [
        Input.text [
            Input.Option.Placeholder placeholder
            Input.Option.OnChange (fun c ->
                dispatch <| UpdateWSModel (fun ws -> {wsModel with SearchString = c.Value.ToUpper()})
            )
        ]
    ]
    

let searchBoxesDummy (wsModel : WaveSimModel) (dispatch : Msg -> unit) : ReactElement =
    Field.div [ Field.IsGrouped ] [
        // 1. Wave name search
        searchBox
            "Search wave names..."
            wsModel
            dispatch

        // 2. Sheet name search
        searchBox
            "Search sheet names..."
            wsModel
            dispatch

        // 3. Component name search
        searchBox
            "Search component names..."
            wsModel
            dispatch

        // 4. Port name search
        searchBox
            "Search port names..."
            wsModel
            dispatch
    ]


let infoButton  : ReactElement =
    div 
        [
            HTMLAttr.ClassName $"{Tooltip.ClassName} {Tooltip.IsMultiline} {Tooltip.IsInfo} {Tooltip.IsTooltipRight}"
            Tooltip.dataTooltip "Find ports by any part of their name. '.' = show all. '*' = show selected. '-' = collapse all"
            Style [FontSize "25px"; MarginTop "0px"; MarginLeft "10px"; Float FloatOptions.Left]] 
        [str Constants.infoSignUnicode]

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


let selectWavesModalHlp25 (wsModel: WaveSimModel) (dispatch: Msg -> unit) (model: Model): ReactElement =
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
    let endModal _ = 
        dispatch <| UpdateWSModel (fun ws ->
            {wsModel with
                WaveModalActive = false
                SearchString = ""
            })
    Modal.modal [
        Modal.IsActive wsModel.WaveModalActive
        Modal.Props [Style [ZIndex 20000]]
    ] [
        Modal.background [
            Props [
                OnClick (fun _ -> dispatch <| UpdateWSModel (fun ws -> {wsModel with WaveModalActive = false}))
            ]
        ] []
        Modal.Card.card [Props [Style [MinWidth "900px"]]] [
            Modal.Card.head [] [
                Modal.Card.title [] [
                    Level.level [] [
                        Level.left [] [ str "Select Waves" ]
                        Level.right [
                        ] [ Delete.delete [
                                Delete.Option.Size IsMedium
                                Delete.Option.OnClick (
                                    fun _ ->
                                        let numWaves = wsModel.SelectedWaves.Length
                                        if numWaves > 50 then
                                            UIPopups.viewWaveSelectConfirmationPopup
                                                50
                                                numWaves
                                                (fun finish _ -> 
                                                        dispatch ClosePopup
                                                        match finish with | true -> endModal() | false -> ()) 
                                                dispatch
                                        else
                                            endModal())
                                    
                                    
                                
                            ] []
                        ]
                    ]
                ]
            ]
            Modal.Card.body [
                Props [
                    Style [
                        OverflowY OverflowOptions.Visible
                        Display DisplayOptions.Grid
                        GridTemplateColumns "1fr 1fr"
                        GridGap "10px"
                        Width "100%"
                    ]
                ]
            ] [
                div [
                    Style [
                        GridColumn "1 / span 2"
                        MarginBottom "15px"
                        Display DisplayOptions.Flex
                        JustifyContent "space-between"
                        AlignItems AlignItemsOptions.Center
                    ]
                ] [
                    // infoButton on the left
                    div [] [ infoButton ]
                    
                    // waves selected message on the right
                    div [] [ str $"{wsModel.SelectedWaves.Length} waves selected" ]
                ]

                // Search bar at the top (spanning both columns)
                div [
                    Style [
                        GridColumn "1 / span 2" 
                        MarginBottom "15px"
                    ]
                ] [ 
                    searchBoxesDummy wsModel dispatch
                ]

                // Left: Wave selector
                div [] [ str "Select Waves Component (placeholder)" ]

                // Right: Breadcrumbs
                div [] [ waveSelectBreadcrumbs wsModel dispatch model ]
            ]

            Modal.Card.foot [Props [Style [Display DisplayOptions.InlineBlock; Float FloatOptions.Right]]]
                [
                    Fulma.Button.button [
                        Fulma.Button.OnClick endModal; 
                        Fulma.Button.Color IsSuccess; 
                        Fulma.Button.Props [Style [Display DisplayOptions.InlineBlock; Float FloatOptions.Right]]
                        ] [str "Done"]
                ]
        ]
    ]
