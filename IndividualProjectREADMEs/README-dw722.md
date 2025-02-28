**GitHub Username: CB-W03**

## Table of Contents

- [How to Run and Test](#how-to-run-and-test)
- [GitHub Perma-Links for Code Components](#github-perma-links-for-code-components)
- [Additional Information (Including Demos)](#additional-information)

This repository contains my contribution to the **HLP25 project**, specifically focusing on improving the **wave selection process** through an enhanced filtering system. My work aims to provide a more efficient and user-friendly way for users to search for **waves, sheets, components, and ports** within the system.  

### Key Features of My Contribution  

#### Advanced Filtering System  
- Allows users to filter waves based on multiple criteria, including:  
  - **Wave Name**  
  - **Sheet Name**  
  - **Component Name**  
  - **Port Name**  
  - **Component Type**  
- Supports **partial text matching**, so users can find relevant results even with incomplete inputs.  
- Uses **optional filtering**, meaning users can specify only the criteria they care about, and the system will adapt accordingly.  

## How to Run and Test

- The code can be located in the `indiv-check-dw722` branch, specifically in the file:  
  **[HLP25CodeBdw722.fs](https://github.com/MaminM/issie/blame/fecd175c6ba7a17251a34e7445266abb660996d3/src/Renderer/UI/WaveSim/HLP25CodeBdw722.fs#L60-L81C6)**

- The updated implementation introduces five search boxes that allow users to filter wave names based on multiple criteria, including:
  - **Wave Name**
  - **Sheet Name**
  - **Component Name**
  - **Port Name**
  - **Component Type**

- The filtering logic ensures that all searches are combined using **AND logic**, meaning all criteria must be met for a wave to appear in the selection.

- The `HighlightedSheets` field provides **visual feedback** to users by marking the sheets that contain matches.

- To test the functionality, I added test functions in `Playground.fs`:
  - `testSearchBoxes`: Creates a test UI with search boxes to verify filtering.

---

## GitHub Perma-Links for Code Components

For assessment purposes, here are permanent links to the relevant code and history:

- **Search Box Implementations:**  
  - **Main Search Boxes:** [View Code](https://github.com/MaminM/issie/blame/fecd175c6ba7a17251a34e7445266abb660996d3/src/Renderer/UI/WaveSim/HLP25CodeBdw722.fs#L60-L81C6)
  - **Sheet Search Box:** [View Code](https://github.com/MaminM/issie/blame/fecd175c6ba7a17251a34e7445266abb660996d3/src/Renderer/UI/WaveSim/HLP25CodeBdw722.fs#L84-L94)  
  - **Component Search Box:** [View Code](https://github.com/MaminM/issie/blame/fecd175c6ba7a17251a34e7445266abb660996d3/src/Renderer/UI/WaveSim/HLP25CodeBdw722.fs#L96-L105C6)  
  - **Port Search Box:** [View Code](https://github.com/MaminM/issie/blame/fecd175c6ba7a17251a34e7445266abb660996d3/src/Renderer/UI/WaveSim/HLP25CodeBdw722.fs#L108-L118)  
  - **Component Type Search Box:** [View Code](https://github.com/MaminM/issie/blame/fecd175c6ba7a17251a34e7445266abb660996d3/src/Renderer/UI/WaveSim/HLP25CodeBdw722.fs#L120-L129C6)  

- **Filtering Function:**  
  - **`filterWaves` Function:** [View Code](https://github.com/MaminM/issie/blame/fecd175c6ba7a17251a34e7445266abb660996d3/src/Renderer/UI/WaveSim/HLP25CodeBdw722.fs#L133-L221C18)  


These links will allow you to see my contributions using GitHub Blame.

---

## Additional Information

- **Context & Design Decisions:**  
  The key improvements in my implementation are:
  - **Modular UI Elements:** Each search box is a reusable function.
  - **Efficient Filtering Logic:** The search criteria are applied dynamically to improve performance.
  - **Enhanced User Experience:** The `HighlightedSheets` feature visually indicates where matches are found.
  - **Logical AND Filtering:** Ensures all selected criteria must be met for a wave to be displayed.

If you need additional clarification or adjustments, feel free to reach out!

---

*This README ensures my contributions can be clearly identified and understood using GitHub blame.*
