# Shop Editor

A basic GUI tool for editing MU Online shop files (`shop.txt`). It relies on `item.txt` for the list of items and optional item images inside `Data/Img/`. The editor is implemented in C# using Windows Forms.

## Requirements
- Mono C# compiler (`mono-devel` package)

## Usage
1. Compile the editor:
   ```bash
   mcs -r:System.Windows.Forms -r:System.Drawing shop_editor.cs
   ```
2. Run it with Mono:
   ```bash
   mono shop_editor.exe
   ```
The interface lets you select an item category from the combo box, choose an item from the list box, and place it on the 8×15 grid. Right click an item on the grid to remove it. Use **Load Shop** to open an existing `shop.txt` file and **Save Shop** to export the current layout. Item images are loaded automatically from `Data/Img/` if present with `.png`, `.jpg`, or `.gif` extensions.
