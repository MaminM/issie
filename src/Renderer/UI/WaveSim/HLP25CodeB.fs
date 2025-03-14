module HLP25CodeB

open Hlp25Types
open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
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
open MiscMenuView
open Constants
open Browser.Types
open WaveSimStyle
open WaveSimHelpers
open SimGraphTypes
open SimTypes
open DiagramStyle

// -----------------------------------------
// Helper Functions & Filtering Logic
// -----------------------------------------

/// Ensures that only valid waves (and selected waves) are returned.
let ensureWaveConsistency (ws: WaveSimModel) =
    let fs = Simulator.getFastSim()
    let okWaves =
        Map.values ws.AllWaves
        |> Seq.toList
        |> List.filter (fun wave -> Map.containsKey wave.WaveId.Id fs.WaveComps)
    if okWaves.Length <> ws.AllWaves.Count then
        printfn "EnsureWaveConsistency: waves,Length=%d, ws.AllWaves.Count=%d" okWaves.Length ws.AllWaves.Count
    let okSelectedWaves =
        ws.SelectedWaves |> List.filter (fun selW -> Map.containsKey selW ws.AllWaves)
    if okSelectedWaves.Length <> ws.SelectedWaves.Length then
        printfn "ok selected waves length = %d <> selectedwaves length = %d" okSelectedWaves.Length ws.SelectedWaves.Length
    okWaves, okSelectedWaves

/// Filtering function that applies an AND operation across all five search criteria.
let filterWaves (wsModel: WaveSimModel) (waves: Wave list) (dispatch: Msg -> unit) =
    let filteredWaves, matchingSheets =
        waves
        |> List.fold (fun (filtered, sheetSet) wave ->
            let addMatchingSheet sheet = Set.add sheet sheetSet

            // Check for sheet name matches.
            let matchesSheet, updatedSheets1 =
                if wsModel.SheetSearchString = "" then true, sheetSet
                else
                    match wave.SubSheet with
                    | [] ->
                        let topSheet = Simulator.getFastSim().SimulatedTopSheet
                        let matches = topSheet.ToUpper().Contains(wsModel.SheetSearchString)
                        if matches then true, addMatchingSheet [] else false, sheetSet
                    | sheets ->
                        let matches = sheets |> List.exists (fun s -> s.ToUpper().Contains(wsModel.SheetSearchString))
                        if matches then true, addMatchingSheet sheets else false, sheetSet

            // Check for component name matches.
            let matchesComponent, updatedSheets2 =
                if wsModel.ComponentSearchString = "" then true, updatedSheets1
                else 
                    let matches = wave.CompLabel.ToUpper().Contains(wsModel.ComponentSearchString)
                    if matches then true, addMatchingSheet wave.SubSheet else false, updatedSheets1

            // Check for port name matches.
            let matchesPort, updatedSheets3 =
                if wsModel.PortSearchString = "" then true, updatedSheets2
                else 
                    let matches = wave.PortLabel.ToUpper().Contains(wsModel.PortSearchString)
                    if matches then true, addMatchingSheet wave.SubSheet else false, updatedSheets2

            // Check for wave name matches.
            let matchesWave, updatedSheets4 =
                if wsModel.WaveSearchString = "" || wsModel.WaveSearchString = "*" then true, updatedSheets3
                else 
                    let matches = wave.ViewerDisplayName.ToUpper().Contains(wsModel.WaveSearchString)
                    if matches then true, addMatchingSheet wave.SubSheet else false, updatedSheets3

            // Check for component type matches.
            let matchesComponentType, updatedSheets5 =
                if wsModel.ComponentTypeSearchString = "" || wsModel.ComponentTypeSearchString = "*" then true, updatedSheets4
                else
                    let fs = Simulator.getFastSim()
                    let comp = fs.WaveComps.[wave.WaveId.Id]
                    let typeStr =
                        match comp.FType with
                        | Not -> "NOT"
                        | Mux2 -> "MUX2"
                        | Demux2 -> "DEMUX2"
                        | DFF -> "DFF"
                        | DFFE -> "DFFE"
                        | Register _ -> "REGISTER"
                        | RegisterE _ -> "REGISTER-E"
                        | ROM1 _ -> "ROM"
                        | RAM1 _ -> "RAM"
                        | AsyncROM1 _ -> "ASYNC-ROM"
                        | AsyncRAM1 _ -> "ASYNC-RAM"
                        | Custom _ -> "CUSTOM"
                        | Input1 _ -> "INPUT"
                        | Output _ -> "OUTPUT"
                        | Constant1 _ -> "CONSTANT"
                        | BusSelection _ -> "BUS-SELECT"
                        | IOLabel -> "IO-LABEL"
                        | _ -> comp.FType.ToString().ToUpper()
                    let matches = typeStr.Contains(wsModel.ComponentTypeSearchString)
                    if matches then true, addMatchingSheet wave.SubSheet else false, updatedSheets4

            if matchesSheet && matchesComponent && matchesPort && matchesWave && matchesComponentType then
                (wave :: filtered, updatedSheets5)
            else
                (filtered, updatedSheets5)
        ) ([], Set.empty)
    // Update the model with highlighted sheets.
    dispatch (UpdateWSModel (fun wsm -> { wsModel with HighlightedSheets = matchingSheets }))
    filteredWaves

// -----------------------------------------
// Search Box UI Components
// -----------------------------------------

/// A style to add some margin between search boxes.
let searchBoxContainerStyle = Style [ MarginRight "10px" ]

/// Search box for wave names.
let waveSearchBox (wsModel: WaveSimModel) (dispatch: Msg -> unit) : ReactElement =
    div [ searchBoxContainerStyle ] [
        Input.text [
            Input.Option.Props [ Style [ MarginBottom "1rem"; Width "100%" ] ]
            Input.Option.Placeholder "Search wave names..."
            Input.Option.OnChange (fun value -> 
                dispatch (UpdateWSModel (fun wsm ->
                    { wsm with
                        WaveSearchString = value.Value.ToUpper()
                        ComponentSearchString = "" // Clear component search when wave search changes.
                        PortSearchString = ""        // Clear port search when wave search changes.
                    }
                ))
            )
        ]
    ]

/// Search box for sheet names.
let sheetSearchBox (wsModel: WaveSimModel) (dispatch: Msg -> unit) : ReactElement =
    div [ searchBoxContainerStyle ] [
        Input.text [
            Input.Option.Value wsModel.SheetSearchString  // Bind current value
            Input.Option.Props [ Style [ MarginBottom "1rem"; Width "100%" ] ]
            Input.Option.Placeholder "Search sheet names..."
            Input.Option.OnChange (fun value ->
                dispatch (UpdateWSModel (fun wsm -> 
                    { wsm with SheetSearchString = value.Value.ToUpper() }
                ))
            )
        ]
    ]

/// Search box for component names.
let componentSearchBox (wsModel: WaveSimModel) (dispatch: Msg -> unit) : ReactElement =
    div [ searchBoxContainerStyle ] [
        Input.text [
            Input.Option.Value wsModel.ComponentSearchString
            Input.Option.Props [ Style [ MarginBottom "1rem"; Width "100%" ] ]
            Input.Option.Placeholder "Search component names..."
            Input.Option.OnChange (fun value ->
                dispatch (UpdateWSModel (fun wsm -> { wsm with ComponentSearchString = value.Value.ToUpper() }))
            )
        ]
    ]

/// Search box for port names.
let portSearchBox (wsModel: WaveSimModel) (dispatch: Msg -> unit) : ReactElement =
    div [ searchBoxContainerStyle ] [
        Input.text [
            Input.Option.Value wsModel.PortSearchString
            Input.Option.Props [ Style [ MarginBottom "1rem"; Width "100%" ] ]
            Input.Option.Placeholder "Search port names..."
            Input.Option.OnChange (fun value ->
                dispatch (UpdateWSModel (fun wsm -> { wsm with PortSearchString = value.Value.ToUpper() }))
            )
        ]
    ]

/// Search box for component types.
let componentTypeSearchBox (wsModel: WaveSimModel) (dispatch: Msg -> unit) : ReactElement =
    div [ searchBoxContainerStyle ] [
        Input.text [
            Input.Option.Props [ Style [ MarginBottom "1rem"; Width "100%" ] ]
            Input.Option.Placeholder "Search component types..."
            Input.Option.OnChange (fun value ->
                dispatch (UpdateWSModel (fun wsm -> { wsm with ComponentTypeSearchString = value.Value.ToUpper() }))
            )
        ]
    ]

// -----------------------------------------
// Breadcrumb Display
// -----------------------------------------

/// Displays a breadcrumb of sheets based on the current search and wave matches.
let waveSelectBreadcrumbs (wsModel: WaveSimModel) (dispatch: Msg -> unit) (model: Model) : ReactElement =
    match model.CurrentProj with
    | None -> div [] [ str "No project open" ]
    | Some project ->
        let updatedProject = ModelHelpers.getUpdatedLoadedComponents project model
        let updatedModel = { model with CurrentProj = Some updatedProject }
        let okWaves, okSelectedWaves = ensureWaveConsistency wsModel
        let filteredWaves =
            match wsModel.WaveSearchString with
            | "" | "-" -> filterWaves wsModel okWaves dispatch
            | "*" ->
                okSelectedWaves
                |> List.map (fun waveId -> wsModel.AllWaves.[waveId])
                |> fun waves -> filterWaves wsModel waves dispatch
            | _ -> filterWaves wsModel okWaves dispatch
        let filteredWaveNames = filteredWaves |> List.map (fun wave -> wave.ViewerDisplayName)
        // Extract sheet names from wave names.
        let sheetNames =
            filteredWaveNames
            |> List.collect (fun name ->
                name.Split('.')
                |> Array.map (fun s -> s.Trim().ToLowerInvariant())
                |> Array.toList)
        let sheetCounts = sheetNames |> List.countBy id
        let sheetColor (sheet: SheetTree) =
            if List.contains (sheet.SheetName.ToLowerInvariant()) sheetNames then 
                IColor.IsCustomColor "pink"
            else 
                IColor.IsCustomColor "darkslategrey"
        let sheetMatches (sheet: SheetTree) =
            match List.tryFind (fun (name, _) -> name = sheet.SheetName.ToLowerInvariant()) sheetCounts with
            | Some (_, count) -> count
            | None -> 0
        let updateSearchStringHelper (sheet: SheetTree) : (Msg -> unit) -> unit =
            fun dispatch ->
                dispatch (UpdateWSModel (fun ws -> { ws with SheetSearchString = sheet.SheetName.ToUpperInvariant() }))
        let breadcrumbConfig = { 
            MiscMenuView.Constants.defaultConfig with
                ClickAction = updateSearchStringHelper
                ColorFun = sheetColor
                NoWaves = sheetMatches
        }
        let breadcrumbs = [
            div [ Style [ TextAlign TextAlignOptions.Center; FontSize "15px" ] ] [ str "Sheets with Design Hierarchy" ]
            MiscMenuView.hierarchyBreadcrumbs breadcrumbConfig dispatch updatedModel
        ]
        div [] breadcrumbs

// -----------------------------------------
// Info Button (for the modal header)
// -----------------------------------------

let infoButton : ReactElement =
    div [
        HTMLAttr.ClassName (sprintf "%s %s %s %s" Tooltip.ClassName Tooltip.IsMultiline Tooltip.IsInfo Tooltip.IsTooltipRight)
        Tooltip.dataTooltip "Find ports by any part of their name. '.' = show all. '*' = show selected. '-' = collapse all"
        Style [ FontSize "25px"; MarginTop "0px"; MarginLeft "10px"; Float FloatOptions.Left ]
    ] [ str Constants.infoSignUnicode ]

// -----------------------------------------
// Wave Selection UI (Left Column)
// -----------------------------------------

// The following functions (toggleSelectAll, toggleWaveSelection, etc.) handle the UI for selecting/deselecting waves.
// (Note: helper functions such as summaryProps, subSheetsToNameReact, isWaveSelected, checkboxInputProps,
//  wavesToIds, details/summary helpers, getCompGroup, GroupItem, summaryName, SheetItem are assumed to exist.)

let toggleSelectAll (selected: bool) (wsModel: WaveSimModel) (dispatch: Msg -> unit) : unit =
    let start = TimeHelpers.getTimeMs ()
    let selectedWaves = if selected then Map.keys wsModel.AllWaves |> Seq.toList else []
    dispatch (GenerateWaveforms { wsModel with SelectedWaves = selectedWaves })
    |> TimeHelpers.instrumentInterval "toggleSelectAll" start

let selectAll (wsModel: WaveSimModel) (dispatch: Msg -> unit) =
    let allWavesSelected = Map.forall (fun index _ -> isWaveSelected index wsModel) wsModel.AllWaves
    tr (summaryProps false (SheetItem []) wsModel dispatch) [
        th [] [
            Checkbox.checkbox [] [
                Checkbox.input [
                    Props (checkboxInputProps @ [
                        Checked allWavesSelected
                        OnChange (fun _ -> toggleSelectAll (not allWavesSelected) wsModel dispatch)
                    ])
                ]
            ]
        ]
        th [] [ str "Select All" ]
    ]

let toggleWaveSelection (index: WaveIndexT) (wsModel: WaveSimModel) (dispatch: Msg -> unit) =
    let selectedWaves =
        if List.contains index wsModel.SelectedWaves then
            List.except [index] wsModel.SelectedWaves
        else
            index :: wsModel.SelectedWaves
    let wsModel' = { wsModel with SelectedWaves = selectedWaves }
    dispatch (GenerateWaveforms wsModel')

let toggleSelectSubGroup (wsModel: WaveSimModel) (dispatch: Msg -> unit) (selected: bool) (waves: WaveIndexT list) =
    let comps = (Simulator.getFastSim()).WaveComps
    let selectedWaves =
        if selected then
            let wavesWithMinDepth =
                if waves = [] then [] else
                    waves
                    |> List.groupBy (fun wave -> comps.[wave.Id].AccessPath.Length)
                    |> List.sort
                    |> List.head
                    |> snd
            List.append wsModel.SelectedWaves wavesWithMinDepth
        else
            List.except waves wsModel.SelectedWaves
    dispatch (GenerateWaveforms { wsModel with SelectedWaves = selectedWaves })

let checkboxRow (wsModel: WaveSimModel) (dispatch: Msg -> unit) (index: WaveIndexT) =
    let fontStyle = if isWaveSelected index wsModel then boldFontStyle else normalFontStyle
    let wave = wsModel.AllWaves.[index]
    tr [fontStyle] [
        td [ noBorderStyle ] [
            Checkbox.checkbox [] [
                Checkbox.input [
                    Props (checkboxInputProps @ [
                        OnChange (fun _ -> toggleWaveSelection index wsModel dispatch)
                        Checked (isWaveSelected index wsModel)
                    ])
                ]
            ]
        ]
        td [ noBorderStyle ] [ str wave.DisplayName ]
    ]

let checkBoxItem wsModel isChecked waveIds dispatch =
    Checkbox.checkbox [] [
        Checkbox.input [
            Props [
                Checked isChecked
                OnChange (fun _ -> toggleSelectSubGroup wsModel dispatch (not isChecked) waveIds)
            ]
        ]
    ]

let waveCheckBoxItem (wsModel: WaveSimModel) (waveIds: WaveIndexT list) dispatch =
    let comps = (Simulator.getFastSim()).WaveComps
    let minDepthSelectedWaves =
        if waveIds = [] then [] else
            waveIds
            |> List.groupBy (fun waveId -> comps.[waveId.Id].AccessPath.Length)
            |> List.sort
            |> List.head
            |> snd
    let checkBoxState = List.exists (fun w -> List.contains w wsModel.SelectedWaves) minDepthSelectedWaves
    Checkbox.checkbox [] [
        Checkbox.input [
            Props [
                Checked checkBoxState
                OnChange (fun _ -> toggleSelectSubGroup wsModel dispatch (not checkBoxState) waveIds)
            ]
        ]
    ]

let makePortRow (ws: WaveSimModel) (dispatch: Msg -> unit) (waves: Wave list) =
    let wave =
        match waves with
        | [wave] -> wave
        | _ -> failwithf "Expected a single wave in port row; got %d" waves.Length
    let subSheet =
        match wave.SubSheet with
        | [] -> str (Simulator.getFastSim().SimulatedTopSheet)
        | _ -> subSheetsToNameReact wave.SubSheet
    tr [] [
        td [] [ waveCheckBoxItem ws [wave.WaveId] dispatch ]
        td [] [ str wave.PortLabel ]
        td [] [ str (match wave.WaveId.PortType with | PortType.Output -> "Output" | PortType.Input -> "Input") ]
    ]

let makeSelectionGroup showDetails (ws: WaveSimModel) (dispatch: Msg -> unit)
      (summaryItem: ReactElement) (rowItems: ReactElement list)
      (cBox: CheckBoxStyle) (waves: Wave list) =
    let wi = wavesToIds waves
    tr (summaryProps false cBox ws dispatch) [
        th [] [ waveCheckBoxItem ws wi dispatch ]
        th [] [
            details (detailsProps showDetails cBox ws dispatch) [
                summary (summaryProps true cBox ws dispatch) [ summaryItem ]
                Table.table [] [ tbody [] rowItems ]
            ]
        ]
    ]

/// Type to output the selected waves and a flag for whether to show detailed view.
type WaveSelectionOutput = {
    WaveList: Wave list
    ShowDetails: bool
}

/// Top-level function to select and filter waves for display.
/// Uses the filtering logic from `filterWaves`.
let selectWavesHlp25 (ws: WaveSimModel) (dispatch: Msg -> unit) : WaveSelectionOutput =
    if not ws.WaveModalActive then 
        { WaveList = []; ShowDetails = false }
    else
        let okWaves, okSelectedWaves = ensureWaveConsistency ws
        let wavesToDisplay =
            match ws.WaveSearchString with
            | "" | "-" -> filterWaves ws okWaves dispatch
            | "*" ->
                okSelectedWaves
                |> List.map (fun wi -> ws.AllWaves.[wi])
                |> fun waves -> filterWaves ws waves dispatch
            | _ -> filterWaves ws okWaves dispatch
        let showDetails = ((List.length wavesToDisplay < 10) || (ws.WaveSearchString.Length > 0))
                          && (ws.WaveSearchString <> "-")
        { WaveList = wavesToDisplay; ShowDetails = showDetails }

let makeFlatGroupRow showDetails (ws: WaveSimModel) (dispatch: Msg -> unit)
      (subSheet: string list) (grp: ComponentGroup) (wavesInGroup: Wave list) =
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
                        OnChange (fun _ -> toggleWaveSelection wave.WaveId ws dispatch)
                        Checked (isWaveSelected wave.WaveId ws)
                    ]
                ]
            ]
        )
    makeSelectionGroup showDetails ws dispatch summaryReact rowItems cBox wavesInGroup

let makeFlatList (ws: WaveSimModel) (dispatch: Msg -> unit)
      (subSheet: string list) (waves: Wave list) (showDetails: bool) =
    let fs = Simulator.getFastSim()
    let groupedBySubSheet =
        waves
        |> List.groupBy (fun w ->
            match w.SubSheet with
            | [] -> "Top-Level"
            | sheetPath -> String.concat "." sheetPath
        )
    let subSheetRows =
        groupedBySubSheet
        |> List.map (fun (subSheetName, wavesInSubSheet) ->
            let componentGroups =
                wavesInSubSheet |> List.groupBy (fun wave -> getCompGroup fs wave)
            let groupRows =
                componentGroups
                |> List.map (fun (grp, groupWaves) ->
                    makeFlatGroupRow showDetails ws dispatch [] grp groupWaves
                )
            makeSelectionGroup showDetails ws dispatch (str subSheetName) groupRows (SheetItem [subSheetName]) wavesInSubSheet
        )
    Table.table [ Table.IsBordered; Table.IsFullWidth; Table.Props [ Style [ BorderWidth 0 ] ] ] [
        tbody [] subSheetRows
    ]

let renderwaves (ws: WaveSimModel) (dispatch: Msg -> unit) (waveselect: WaveSelectionOutput) : ReactElement =
    let showDetails = waveselect.ShowDetails
    let wavelist = waveselect.WaveList
    // Use the new flat approach
    makeFlatList ws dispatch [] wavelist showDetails

// -----------------------------------------
// Modal Display for Wave Selection
// -----------------------------------------

/// Displays the modal for wave selection. The top row shows the info button and wave count,
/// below it a horizontal row of search boxes (from HLP25CodeBdw722) is displayed,
/// and then a two‑column grid shows the wave selection (left) and breadcrumbs (right).
let selectWavesModalHlp25 (wsModel: WaveSimModel) (dispatch: Msg -> unit) (model: Model) : ReactElement =
    // Helper to close the modal and reset search string.
    let resetSearchFilters (ws: WaveSimModel) =
        { ws with 
             WaveSearchString = ""
             SheetSearchString = ""
             ComponentSearchString = ""
             PortSearchString = ""
             ComponentTypeSearchString = ""
             HighlightedSheets = Set.empty
        }

    let closeModal () =
        dispatch (UpdateWSModel (fun ws -> 
            resetSearchFilters { ws with WaveModalActive = false }
        ))

    // Handler for closing the modal (with confirmation if >50 waves are selected).
    let handleModalClose _ =
        let numWaves = List.length wsModel.SelectedWaves
        if numWaves > 50 then
            UIPopups.viewWaveSelectConfirmationPopup
                50
                numWaves
                (fun finish _ ->
                    dispatch ClosePopup
                    if finish then closeModal ())
                dispatch
        else
            closeModal ()
        // Always reset the search string.
        dispatch (UpdateWSModel (fun ws -> { ws with SearchString = "" }))
    Modal.modal [
        Modal.IsActive wsModel.WaveModalActive
        Modal.Props [ Style [ ZIndex 20000 ] ]
    ] [
        // Modal background to allow closing on click.
        Modal.background [
            Props [ OnClick (fun _ -> dispatch (UpdateWSModel (fun ws -> { ws with WaveModalActive = false }))) ]
        ] []
        // Main modal card.
        Modal.Card.card [ Props [ Style [ MinWidth "80%" ] ] ] [
            // Header with title and delete button.
            Modal.Card.head [] [
                Modal.Card.title [] [
                    Level.level [] [
                        Level.left [] [ str "Select Waves" ]
                        Level.right [] [
                            Delete.delete [
                                Delete.Option.Size IsMedium
                                Delete.Option.OnClick handleModalClose
                            ] []
                        ]
                    ]
                ]
            ]
            // Body with info row, search boxes row, then two columns for selection and breadcrumbs.
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
                // Top row: info button and waves count.
                div [
                    Style [
                        GridColumn "1 / span 2"
                        MarginBottom "15px"
                        Display DisplayOptions.Flex
                        JustifyContent "space-between"
                        AlignItems AlignItemsOptions.Center
                    ]
                ] [
                    div [] [ infoButton ]
                    div [] [ str (sprintf "%d waves selected" (List.length wsModel.SelectedWaves)) ]
                ]
                // Search boxes row: arranged horizontally with wrapping if needed.
                div [
                    Style [
                        GridColumn "1 / span 2"
                        MarginBottom "15px"
                        Display DisplayOptions.Flex
                        FlexDirection "row"
                        FlexWrap "wrap"
                    ]
                ] [
                    waveSearchBox wsModel dispatch
                    sheetSearchBox wsModel dispatch
                    componentSearchBox wsModel dispatch
                    portSearchBox wsModel dispatch
                    componentTypeSearchBox wsModel dispatch
                ]
                // Left column: Wave selection component.
                div [] [
                    let waveselect = selectWavesHlp25 wsModel dispatch
                    renderwaves wsModel dispatch waveselect
                ]
                // Right column: Breadcrumb display.
                div [] [ waveSelectBreadcrumbs wsModel dispatch model ]
            ]
            // Footer with Done button.
            Modal.Card.foot [ Props [ Style [ Display DisplayOptions.InlineBlock; Float FloatOptions.Right ] ] ] [
                Fulma.Button.button [
                    Fulma.Button.OnClick (fun _ -> closeModal ())
                    Fulma.Button.Color IsSuccess
                    Fulma.Button.Props [ Style [ Display DisplayOptions.InlineBlock; Float FloatOptions.Right ] ]
                ] [ str "Done" ]
            ]
        ]
    ]



