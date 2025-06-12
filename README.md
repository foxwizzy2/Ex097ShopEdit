# Shop Editor

A basic GUI tool for editing MU Online shop files (`shop.txt`). It relies on `item.txt` for the list of items and optional item images inside `Data/Img/`.

## Requirements
- Python 3 with `tkinter`
- `Pillow` (`pip install pillow`)
- Mono C# compiler (`mono-devel` package) for the C# version

## Usage (Python)
1. Run the editor:
   ```bash
   python3 shop_editor.py
   ```
2. Select an item category from the combo box on top.
3. Choose an item from the list box and left click on the grid to place it. Right click an item to remove it.
4. Use **Load Shop** to open an existing `shop.txt` file. Use **Save Shop** to export the current layout.

Images are loaded automatically if files with the item name (spaces replaced by underscores) exist in `Data/Img/` with `.png`, `.jpg` or `.gif` extension.

## Usage (C#)
1. Compile the editor:
   ```bash
   mcs -r:System.Windows.Forms -r:System.Drawing shop_editor.cs
   ```
2. Run it with Mono:
   ```bash
   mono shop_editor.exe
   ```

The interface is similar to the Python version and allows loading and saving `shop.txt` files.
