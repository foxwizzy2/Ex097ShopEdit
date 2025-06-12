# Shop Editor

A basic GUI tool for editing MU Online shop files (`shop.txt`). The program reads `item.txt` for the list of items and optionally loads item images from `Data/Img/`. It is implemented using Windows Forms.

## Building with Visual Studio 2022
1. Open `ShopEditor.csproj` in Visual Studio 2022.
2. Build the project (it targets .NET 8 with Windows Forms).
3. Run the resulting executable. The `Data` directory is copied automatically to the output so item definitions are found.

You can also build from the command line with the .NET SDK:
```bash
dotnet build -c Release
```
The executable will be under `bin/Release/net8.0-windows/`.

## Usage
The interface lets you select an item category from the combo box, choose an item from the list box, and place it on the 8×15 grid. Right click a placed item to remove it. Use **Load Shop** to open an existing `shop.txt` file and **Save Shop** to export the current layout. Item images are loaded automatically from `Data/Img/` if present with `.png`, `.jpg`, or `.gif` extensions.
