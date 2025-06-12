using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

public class ShopEditor : Form
{
    private ComboBox categoryCombo;
    private ListBox itemList;
    private DataGridView grid;
    private Button loadButton;
    private Button saveButton;
    private Dictionary<int, Category> categories;
    private Item selectedItem;
    private GridEntry[,] gridData = new GridEntry[15,8];

    public ShopEditor()
    {
        Text = "Shop Editor";
        Width = 800;
        Height = 600;
        InitComponents();
        categories = ItemParser.Parse("Data/item.txt");
        foreach(var kv in categories.OrderBy(k=>k.Key))
        {
            categoryCombo.Items.Add($"{kv.Key} - {kv.Value.Name}");
        }
        if(categoryCombo.Items.Count>0) categoryCombo.SelectedIndex = 0;
        UpdateItemList();
    }

    private void InitComponents()
    {
        categoryCombo = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
        categoryCombo.SelectedIndexChanged += (s,e)=> UpdateItemList();
        itemList = new ListBox { Dock = DockStyle.Top, Height = 120 };
        itemList.SelectedIndexChanged += ItemList_SelectedIndexChanged;
        grid = new DataGridView {
            Dock = DockStyle.Fill,
            ColumnCount = 8,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeColumns = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            RowHeadersVisible = false,
            ColumnHeadersVisible = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            BackgroundColor = Color.DimGray,
            EnableHeadersVisualStyles = false
        };
        grid.DefaultCellStyle.BackColor = Color.DimGray;
        grid.RowTemplate.Height = 32;
        grid.RowCount = 15;
        grid.CellClick += Grid_CellClick;
        grid.CellMouseDown += Grid_CellMouseDown;
        for(int i=0;i<8;i++)
            grid.Columns[i].Width = 32;
        loadButton = new Button { Text = "Load Shop" };
        loadButton.Click += (s,e)=>LoadShop();
        saveButton = new Button { Text = "Save Shop" };
        saveButton.Click += (s,e)=>SaveShop();
        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        buttonPanel.Controls.Add(loadButton);
        buttonPanel.Controls.Add(saveButton);
        Controls.Add(grid);
        Controls.Add(buttonPanel);
        Controls.Add(itemList);
        Controls.Add(categoryCombo);
    }

    private void UpdateItemList()
    {
        itemList.Items.Clear();
        if(categoryCombo.SelectedIndex<0) return;
        int cid = GetSelectedCategoryId();
        foreach(var item in categories[cid].Items)
        {
            itemList.Items.Add($"{item.Index:D3} {item.Name}");
        }
    }

    private int GetSelectedCategoryId()
    {
        var text = categoryCombo.SelectedItem.ToString();
        var parts = text.Split(' ');
        int cid=0; int.TryParse(parts[0], out cid); return cid;
    }

    private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        if(e.RowIndex<0||e.ColumnIndex<0) return;
        if(selectedItem==null)
        {
            var entry = gridData[e.RowIndex,e.ColumnIndex];
            if(entry!=null) RemoveEntry(entry);
            return;
        }
        PlaceItem(e.ColumnIndex,e.RowIndex,selectedItem);
    }

    private void Grid_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
    {
        if(e.Button==MouseButtons.Right && e.RowIndex>=0 && e.ColumnIndex>=0)
        {
            var entry = gridData[e.RowIndex,e.ColumnIndex];
            if(entry!=null) RemoveEntry(entry);
        }
    }

    private void PlaceItem(int gx, int gy, Item item)
    {
        if(gx+item.Width>8 || gy+item.Height>15) return;
        for(int y=gy;y<gy+item.Height;y++)
            for(int x=gx;x<gx+item.Width;x++)
                if(gridData[y,x]!=null) return;
        var entry = new GridEntry{Item=item,X=gx,Y=gy};
        for(int y=gy;y<gy+item.Height;y++)
            for(int x=gx;x<gx+item.Width;x++)
            {
                gridData[y,x]=entry;
                grid.Rows[y].Cells[x].Style.BackColor = Color.LightBlue;
                if(y==gy && x==gx)
                    grid.Rows[y].Cells[x].Value = item.Name.Substring(0,Math.Min(4,item.Name.Length));
                else
                    grid.Rows[y].Cells[x].Value = "";
            }
    }

    private void RemoveEntry(GridEntry entry)
    {
        for(int y=entry.Y;y<entry.Y+entry.Item.Height;y++)
            for(int x=entry.X;x<entry.X+entry.Item.Width;x++)
            {
                gridData[y,x]=null;
                grid.Rows[y].Cells[x].Value = null;
                grid.Rows[y].Cells[x].Style.BackColor = Color.DimGray;
            }
    }

    private void ItemList_SelectedIndexChanged(object sender, EventArgs ev)
    {
        var idx = itemList.SelectedIndex;
        if(idx>=0)
        {
            int cid = GetSelectedCategoryId();
            selectedItem = categories[cid].Items[idx];
        }
        else
        {
            selectedItem = null;
        }
    }

    private void LoadShop()
    {
        var dlg = new OpenFileDialog{Filter="Text|*.txt"};
        if(dlg.ShowDialog()!=DialogResult.OK) return;
        ClearGrid();
        foreach(var entry in ShopParser.Parse(dlg.FileName))
        {
            if(categories.TryGetValue(entry.Category,out var cat))
            {
                var item = cat.Items.FirstOrDefault(i=>i.Index==entry.Index);
                if(item!=null) PlaceItem(entry.X,entry.Y,item);
            }
        }
    }

    private void SaveShop()
    {
        var dlg = new SaveFileDialog{Filter="Text|*.txt"};
        if(dlg.ShowDialog()!=DialogResult.OK) return;
        using(var sw = new StreamWriter(dlg.FileName))
        {
            sw.WriteLine("// Shop generated by ShopEditor");
            sw.WriteLine("//Index\tLevel\tDur\tSkill\tLuck\tOption\tExcOp\tSlotX\tSlotY\tComment");
            HashSet<GridEntry> written = new HashSet<GridEntry>();
            for(int y=0;y<15;y++)
            for(int x=0;x<8;x++)
            {
                var entry = gridData[y,x];
                if(entry!=null && !written.Contains(entry))
                {
                    written.Add(entry);
                    sw.WriteLine($"{entry.Item.Category:D2},{entry.Item.Index:D3}\t0\t*\t0\t0\t0\t0\t{entry.X}\t{entry.Y}\t//{entry.Item.Name}");
                }
            }
            sw.WriteLine("end");
        }
    }

    private void ClearGrid()
    {
        for(int y=0;y<15;y++)
            for(int x=0;x<8;x++)
            {
                gridData[y,x]=null;
                grid.Rows[y].Cells[x].Value=null;
                grid.Rows[y].Cells[x].Style.BackColor = Color.DimGray;
            }
    }

    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.Run(new ShopEditor());
    }
}

public class GridEntry
{
    public Item Item;
    public int X;
    public int Y;
}

public class Item
{
    public int Index;
    public int Width;
    public int Height;
    public string Name;
    public int Category;
}

public class Category
{
    public int Id;
    public string Name;
    public List<Item> Items = new List<Item>();
}

public static class ItemParser
{
    public static Dictionary<int,Category> Parse(string path)
    {
        var lines = File.ReadAllLines(path);
        var cats = new Dictionary<int,Category>();
        string catName = null;
        int catId = -1;
        foreach(var raw in lines)
        {
            var line = raw.Trim();
            if(line.StartsWith("//") && line.Contains("===="))
                continue;
            if(line.StartsWith("//"))
            {
                catName = line.Trim('/',' ','\t');
                continue;
            }
            if(line=="end")
            {
                catName=null; catId=-1; continue;
            }
            if(string.IsNullOrWhiteSpace(line)) continue;
            if(catId==-1 && int.TryParse(line,out catId))
            {
                var cat = new Category{Id=catId, Name=catName};
                cats[catId]=cat; continue;
            }
            if(catId!=-1)
            {
                var parts = line.Split('"');
                if(parts.Length<2) continue;
                string before = parts[0];
                string name = parts[1];
                var nums = before.Split(new char[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);
                if(nums.Length>=5)
                {
                    int idx=int.Parse(nums[0]);
                    int width=int.Parse(nums[3]);
                    int height=int.Parse(nums[4]);
                    cats[catId].Items.Add(new Item{Index=idx,Width=width,Height=height,Name=name,Category=catId});
                }
            }
        }
        return cats;
    }
}

public class ShopEntry
{
    public int Category;
    public int Index;
    public int X;
    public int Y;
}

public static class ShopParser
{
    public static IEnumerable<ShopEntry> Parse(string path)
    {
        foreach(var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if(string.IsNullOrWhiteSpace(line) || line.StartsWith("//") || line=="end") continue;
            var basePart = line.Split(new[]{'/'},2)[0];
            var parts = basePart.Replace(',', ' ').Split(new char[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);
            if(parts.Length<9) continue;
            if(!int.TryParse(parts[0],out int cat)) continue;
            if(!int.TryParse(parts[1],out int idx)) continue;
            if(!int.TryParse(parts[7],out int x)) continue;
            if(!int.TryParse(parts[8],out int y)) continue;
            yield return new ShopEntry{Category=cat,Index=idx,X=x,Y=y};
        }
    }
}
