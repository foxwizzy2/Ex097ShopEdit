import os
import re
import tkinter as tk
from tkinter import ttk, filedialog, messagebox

try:  # optional Pillow dependency
    from PIL import Image, ImageTk
except Exception:  # Pillow missing or failed to load
    Image = None
    ImageTk = None

CELL_SIZE = 32
GRID_W = 8
GRID_H = 15


def parse_item_file(path):
    categories = {}
    current_name = None
    current_id = None
    with open(path, 'r') as f:
        for raw in f:
            line = raw.strip()
            if not line:
                continue
            if line.startswith('//'):
                text = line.strip('/').strip()
                if '====' in text or text.startswith('Index'):
                    continue
                if text:
                    current_name = text
                continue
            if re.fullmatch(r'\d+', line):
                current_id = int(line)
                categories[current_id] = {'name': current_name, 'items': []}
                continue
            if line.startswith('end'):
                current_id = None
                continue
            if current_id is not None:
                # split by quotes to extract name
                parts = line.split('"')
                name = parts[1] if len(parts) > 1 else 'Unknown'
                numbers = parts[0].strip().split()
                if len(numbers) >= 5:
                    index = int(numbers[0])
                    width = int(numbers[3])
                    height = int(numbers[4])
                    categories[current_id]['items'].append({
                        'index': index,
                        'name': name,
                        'width': width,
                        'height': height
                    })
    return categories


def parse_shop_file(path):
    entries = []
    with open(path, 'r') as f:
        for raw in f:
            line = raw.strip()
            if not line or line.startswith('//') or line.startswith('end'):
                continue
            base = line.split('//')[0]
            parts = base.replace(',', ' ').split()
            if len(parts) < 2:
                continue
            try:
                cat_id = int(parts[0])
                idx = int(parts[1])
            except ValueError:
                continue
            slotx = None
            sloty = None
            if len(parts) >= 9:
                if parts[7] != '*':
                    slotx = int(parts[7])
                if parts[8] != '*':
                    sloty = int(parts[8])
            entries.append({'category': cat_id, 'index': idx, 'x': slotx, 'y': sloty})
    return entries


def load_image_for(item):
    if Image is None:
        return None
    name = item['name'].replace(' ', '_')
    for ext in ('png', 'jpg', 'gif'):
        path = os.path.join('Data', 'Img', f"{name}.{ext}")
        if os.path.exists(path):
            img = Image.open(path)
            img = img.resize((item['width'] * CELL_SIZE, item['height'] * CELL_SIZE))
            return ImageTk.PhotoImage(img)
    return None


class ShopEditor(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title('Shop Editor')
        self.items_data = parse_item_file(os.path.join('Data', 'item.txt'))
        self.category_var = tk.StringVar()
        self.category_combo = ttk.Combobox(self, textvariable=self.category_var, state='readonly')
        values = [f"{cid} - {v['name']}" for cid, v in sorted(self.items_data.items())]
        self.category_combo['values'] = values
        if self.category_combo['values']:
            self.category_combo.current(0)
        self.category_combo.pack(fill='x')
        self.category_combo.bind('<<ComboboxSelected>>', self.update_item_list)

        self.item_list = tk.Listbox(self, height=8)
        self.item_list.pack(fill='both')

        btn_frame = tk.Frame(self)
        btn_frame.pack(fill='x')
        tk.Button(btn_frame, text='Load Shop', command=self.load_shop).pack(side='left')
        tk.Button(btn_frame, text='Save Shop', command=self.save_shop).pack(side='left')
        tk.Button(btn_frame, text='Clear', command=self.clear_grid).pack(side='left')

        self.canvas = tk.Canvas(self, width=GRID_W*CELL_SIZE, height=GRID_H*CELL_SIZE, bg='white')
        self.canvas.pack(side='right', padx=5, pady=5)
        for x in range(GRID_W+1):
            self.canvas.create_line(x*CELL_SIZE, 0, x*CELL_SIZE, GRID_H*CELL_SIZE, fill='gray')
        for y in range(GRID_H+1):
            self.canvas.create_line(0, y*CELL_SIZE, GRID_W*CELL_SIZE, y*CELL_SIZE, fill='gray')
        self.canvas.bind('<Button-1>', self.place_item_event)
        self.canvas.bind('<Button-3>', self.remove_item_event)

        self.images = []
        self.grid_data = [[None for _ in range(GRID_W)] for _ in range(GRID_H)]
        self.update_item_list()

    def get_selected_category_id(self):
        sel = self.category_combo.get()
        return int(sel.split(' ')[0])

    def update_item_list(self, event=None):
        cid = self.get_selected_category_id()
        self.item_list.delete(0, tk.END)
        for it in self.items_data[cid]['items']:
            self.item_list.insert(tk.END, f"{it['index']:03d} {it['name']}")

    def place_item_event(self, event):
        selection = self.item_list.curselection()
        if not selection:
            return
        cid = self.get_selected_category_id()
        item = self.items_data[cid]['items'][selection[0]]
        gx = event.x // CELL_SIZE
        gy = event.y // CELL_SIZE
        if gx + item['width'] > GRID_W or gy + item['height'] > GRID_H:
            messagebox.showerror('Error', 'Item outside grid')
            return
        for y in range(gy, gy + item['height']):
            for x in range(gx, gx + item['width']):
                if self.grid_data[y][x] is not None:
                    messagebox.showerror('Error', 'Space occupied')
                    return
        img = load_image_for(item)
        if img:
            obj = self.canvas.create_image(gx*CELL_SIZE, gy*CELL_SIZE, anchor='nw', image=img)
            self.images.append(img)
        else:
            obj = self.canvas.create_rectangle(gx*CELL_SIZE, gy*CELL_SIZE,
                                               (gx+item['width'])*CELL_SIZE,
                                               (gy+item['height'])*CELL_SIZE,
                                               fill='skyblue')
            self.canvas.create_text((gx+item['width']/2)*CELL_SIZE,
                                    (gy+item['height']/2)*CELL_SIZE,
                                    text=item['name'][:4], font=('Arial', 8))
        entry = {'category': cid, 'index': item['index'],
                 'x': gx, 'y': gy, 'width': item['width'], 'height': item['height'],
                 'obj': obj}
        for y in range(gy, gy + item['height']):
            for x in range(gx, gx + item['width']):
                self.grid_data[y][x] = entry

    def remove_item_event(self, event):
        gx = event.x // CELL_SIZE
        gy = event.y // CELL_SIZE
        entry = self.grid_data[gy][gx]
        if not entry:
            return
        self.canvas.delete(entry['obj'])
        for y in range(entry['y'], entry['y'] + entry['height']):
            for x in range(entry['x'], entry['x'] + entry['width']):
                self.grid_data[y][x] = None

    def clear_grid(self):
        self.canvas.delete('all')
        for x in range(GRID_W+1):
            self.canvas.create_line(x*CELL_SIZE, 0, x*CELL_SIZE, GRID_H*CELL_SIZE, fill='gray')
        for y in range(GRID_H+1):
            self.canvas.create_line(0, y*CELL_SIZE, GRID_W*CELL_SIZE, y*CELL_SIZE, fill='gray')
        self.grid_data = [[None for _ in range(GRID_W)] for _ in range(GRID_H)]
        self.images.clear()

    def load_shop(self):
        path = filedialog.askopenfilename(title='Open shop', filetypes=[('Text','*.txt')])
        if not path:
            return
        self.clear_grid()
        for e in parse_shop_file(path):
            if e['x'] is None or e['y'] is None:
                continue
            cat = self.items_data.get(e['category'])
            if not cat:
                continue
            item = next((i for i in cat['items'] if i['index']==e['index']), None)
            if not item:
                continue
            # emulate placing
            gx, gy = e['x'], e['y']
            if gx + item['width'] > GRID_W or gy + item['height'] > GRID_H:
                continue
            occupied = False
            for y in range(gy, gy + item['height']):
                for x in range(gx, gx + item['width']):
                    if self.grid_data[y][x] is not None:
                        occupied = True
                        break
                if occupied:
                    break
            if occupied:
                continue
            img = load_image_for(item)
            if img:
                obj = self.canvas.create_image(gx*CELL_SIZE, gy*CELL_SIZE, anchor='nw', image=img)
                self.images.append(img)
            else:
                obj = self.canvas.create_rectangle(gx*CELL_SIZE, gy*CELL_SIZE,
                                                   (gx+item['width'])*CELL_SIZE,
                                                   (gy+item['height'])*CELL_SIZE,
                                                   fill='skyblue')
                self.canvas.create_text((gx+item['width']/2)*CELL_SIZE,
                                        (gy+item['height']/2)*CELL_SIZE,
                                        text=item['name'][:4], font=('Arial', 8))
            entry = {'category': e['category'], 'index': item['index'],
                     'x': gx, 'y': gy, 'width': item['width'], 'height': item['height'],
                     'obj': obj}
            for y in range(gy, gy + item['height']):
                for x in range(gx, gx + item['width']):
                    self.grid_data[y][x] = entry
        messagebox.showinfo('Loaded', f'Shop loaded from {path}')

    def save_shop(self):
        path = filedialog.asksaveasfilename(defaultextension='.txt', title='Save shop')
        if not path:
            return
        with open(path, 'w') as f:
            f.write('// Shop generated by ShopEditor\n')
            f.write('//Index\tLevel\tDur\tSkill\tLuck\tOption\tExcOp\tSlotX\tSlotY\tComment\n')
            used = set()
            for y in range(GRID_H):
                for x in range(GRID_W):
                    entry = self.grid_data[y][x]
                    if entry and entry not in used:
                        used.add(entry)
                        name = next((i['name'] for i in self.items_data[entry['category']]['items'] if i['index']==entry['index']), 'Item')
                        f.write(f"{entry['category']:02d},{entry['index']:03d}\t0\t*\t0\t0\t0\t0\t{entry['x']}\t{entry['y']}\t//{name}\n")
            f.write('end\n')
        messagebox.showinfo('Saved', f'Shop saved to {path}')


if __name__ == '__main__':
    app = ShopEditor()
    app.mainloop()
