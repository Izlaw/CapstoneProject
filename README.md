# ThreadStudio — 3D Shirt Designer (Capstone Project)

> **Conversation context:** This project was scaffolded via AI assistant. Open this folder as a workspace and continue building from where we left off.

---

## 🧵 Project Overview

**ThreadStudio** is a Blazor WebAssembly + MudBlazor e-commerce-style app where users can:
- Design a 3D shirt in real time (color, text, logo)
- Export their design as a PNG
- Save designs to Supabase (in progress)

---

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Blazor WASM (.NET 9) |
| UI Components | MudBlazor 8.6.0 |
| 3D Rendering | Three.js r0.168 (via importmap CDN) |
| 2D Canvas Texture | Fabric.js 5.3.1 (CDN) |
| Backend / Storage | Supabase (BaaS) |
| Font | Outfit (Google Fonts) |

---

## 📁 Project Structure

```
CapstoneProject/
├── Program.cs                          ← MudBlazor + services registered
├── _Imports.razor                      ← Global usings (MudBlazor, Services, Models)
├── App.razor
│
├── Layout/
│   └── MainLayout.razor               ← MudBlazor dark theme + AppBar
│
├── Pages/
│   ├── Index.razor                    ← Landing/hero page
│   └── Designer/
│       └── Designer.razor             ← Main 3D designer page
│
├── Components/
│   ├── ShirtCanvas.razor              ← JS Interop wrapper for Three.js scene
│   ├── DesignToolbar.razor            ← Color, text, logo tool panels
│   └── ExportPanel.razor              ← Export PNG + Save buttons
│
├── Models/
│   └── ShirtDesign.cs                 ← Design state model
│
├── Services/
│   ├── DesignService.cs               ← In-memory design state manager
│   ├── ExportService.cs               ← PNG export via JSInterop
│   └── SupabaseService.cs             ← Supabase HTTP client stub
│
└── wwwroot/
    ├── index.html                     ← Three.js importmap, Fabric.js CDN, fonts
    ├── models/
    │   └── shirt.glb                  ← UV-mapped 3D shirt (from JS Mastery OSS)
    ├── js/
    │   └── shirt-designer.js          ← Three.js + OrbitControls + Fabric texture
    └── css/
        └── app.css                    ← Full dark theme (purple/cyan palette)
```

---

## 🚀 Running the App

```powershell
cd C:\Users\Thirdy\Desktop\myRepo\CapstoneProject
dotnet run
```

Then open: **https://localhost:5001** (or whatever port is shown)

---

## ✅ What's Done

- [x] Blazor WASM project scaffolded (.NET 9)
- [x] MudBlazor 8.6.0 installed and configured
- [x] Dark MudBlazor theme (purple `#7c3aed` / cyan `#06b6d4`)
- [x] `index.html` — Three.js importmap, Fabric.js CDN, Outfit font, premium loading screen
- [x] `app.css` — full dark theme with hero, floating orbs, feature cards, designer layout
- [x] `Program.cs` — all services registered
- [x] `MainLayout.razor` — glassmorphism AppBar + MudBlazor providers
- [x] `Pages/Index.razor` — hero landing page with features grid
- [x] `Pages/Designer/Designer.razor` — wires all components together
- [x] `Components/ShirtCanvas.razor` — JS module isolation via `IJSObjectReference`
- [x] `Components/DesignToolbar.razor` — color swatches, text tool, logo upload, actions
- [x] `Components/ExportPanel.razor` — export PNG + save buttons
- [x] `Models/ShirtDesign.cs`
- [x] `Services/DesignService.cs`
- [x] `Services/ExportService.cs`
- [x] `Services/SupabaseService.cs` (stub — keys wired in)
- [x] `wwwroot/js/shirt-designer.js` — Three.js module (scene, UV texture, controls, export)
- [x] `wwwroot/models/shirt.glb` — UV-mapped shirt model

---

## 🔧 What's Still Needed (Next Steps)

### High Priority
- [x] **Test the build** — `dotnet build` then `dotnet run` to verify no compile errors
- [ ] **Verify 3D rendering** — confirm `shirt.glb` loads and Three.js renders correctly
- [ ] **Supabase: create `designs` table** in the dashboard (schema below)
- [x] **Wire up `HandleSave()`** in Designer.razor to actually call `SupabaseService`

### Medium Priority
- [x] **NavMenu cleanup** — remove the default `NavMenu.razor` (unused now)
- [x] **Counter.razor / Weather.razor** — delete the boilerplate pages from template
- [x] **Gallery page** — show saved designs from Supabase
- [x] **Multiple shirt views** — front, back, sleeves tabs
- [x] **Shirt size selector** — S, M, L, XL for order simulation

### Nice to Have
- [x] **Supabase Auth** — GitHub / email login
- [ ] **Save texture to Supabase Storage** — upload PNG to `design-textures` bucket
- [ ] **Shop/e-commerce page** — product listing cards
- [ ] **Checkout flow** — size, quantity, order submission
- [x] **Toast notifications** — success/error feedback with MudSnackbar

---

## 🗄️ Supabase Setup

**Project URL:** `https://jvljcrwazmlcjkqomwdz.supabase.co`  
**Anon Key:** stored in `Services/SupabaseService.cs`

### Required: Create `designs` table in Supabase SQL Editor

```sql
CREATE TABLE designs (
  id          uuid DEFAULT gen_random_uuid() PRIMARY KEY,
  user_id     uuid REFERENCES auth.users(id),
  name        text NOT NULL DEFAULT 'My Design',
  shirt_color text NOT NULL DEFAULT '#ffffff',
  text_overlay text,
  texture_url text,
  created_at  timestamptz DEFAULT now()
);

ALTER TABLE designs ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Anyone can insert" ON designs FOR INSERT WITH CHECK (true);
CREATE POLICY "Anyone can read"   ON designs FOR SELECT USING (true);
```

### Optional: Storage bucket for textures
```
Bucket name: design-textures
Public: true
```

---

## 🎨 Design Aesthetic

- **Theme:** Dark streetwear-inspired
- **Primary color:** `#7c3aed` (Electric Purple)
- **Secondary color:** `#06b6d4` (Cyan)
- **Font:** Outfit (Google Fonts)
- **Motifs:** Glassmorphism, floating orbs, grid background, gradient text

---

## ⚙️ How the 3D Texture Works

```
Fabric.js canvas (512×512, hidden)
         │
         │  ← user paints color / text / logo here
         ▼
THREE.CanvasTexture(fabricCanvas.getElement())
         │
         │  ← applied to MeshStandardMaterial.map
         ▼
Three.js shirt mesh (UV-mapped shirt.glb)
         │
         ▼
OrbitControls — user can drag/zoom to inspect
```

Each Fabric.js `after:render` event sets `threeTexture.needsUpdate = true`,
which Three.js picks up on the next animation frame.

---

## 🔗 Key References

- [MudBlazor Docs](https://mudblazor.com/)
- [Three.js Docs](https://threejs.org/docs/)
- [Fabric.js Docs](http://fabricjs.com/docs/)
- [Supabase JS Client](https://supabase.com/docs/reference/javascript/introduction)
- [shirt.glb source](https://github.com/adrianhajdin/project_threejs_ai) — JS Mastery (educational use)
