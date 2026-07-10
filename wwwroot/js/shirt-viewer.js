import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { DecalGeometry } from 'three/addons/geometries/DecalGeometry.js';

let scene, camera, renderer, controls, shirtGroup;
let currentColor = '#ffffff';
let blazorRef = null;

const decals = [];
let activeDecal = null;

const dragPreviewMesh = new THREE.Mesh(
    new THREE.PlaneGeometry(0.6, 0.6),
    new THREE.MeshBasicMaterial({ transparent: true, depthTest: false, side: THREE.DoubleSide })
);
dragPreviewMesh.visible = false;


const decalMaterial = new THREE.MeshStandardMaterial({
    transparent: true,
    depthTest: true,
    depthWrite: false,
    polygonOffset: true,
    polygonOffsetFactor: -4,
    roughness: 0.8,
    metalness: 0.1,
});

export function init(containerId, dummyCanvasId, dotnetHelper) {
    blazorRef = dotnetHelper;
    
    // ── Reset Global State for component re-use ──────────
    decals.length = 0;
    activeDecal = null;
    shirtGroup = null;
    currentColor = '#ffffff';
    
    const container = document.getElementById(containerId);
    if (!container) {
        console.error("Canvas container not found");
        return;
    }

    // ── Scene Setup ────────────────────────────────────────
    scene = new THREE.Scene();
    scene.background = new THREE.Color(0xf5f5f5);
    scene.add(dragPreviewMesh);

    camera = new THREE.PerspectiveCamera(45, container.clientWidth / container.clientHeight, 0.1, 100);
    camera.position.set(0, 0, 3);

    renderer = new THREE.WebGLRenderer({ antialias: true, preserveDrawingBuffer: true });
    renderer.setSize(container.clientWidth, container.clientHeight);
    renderer.setPixelRatio(window.devicePixelRatio);
    renderer.shadowMap.enabled = true;
    container.appendChild(renderer.domElement);

    controls = new OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;
    controls.dampingFactor = 0.05;
    controls.minDistance = 1.5;
    controls.maxDistance = 4.5;
    controls.enablePan = false;
    controls.target.set(0, 0, 0);

    // ── Lighting ───────────────────────────────────────────
    scene.add(new THREE.AmbientLight(0xffffff, 0.7));

    const keyLight = new THREE.DirectionalLight(0xffffff, 2.5);
    keyLight.position.set(3, 4, 3);
    scene.add(keyLight);

    const fillLight = new THREE.DirectionalLight(0xFDA481, 0.8);
    fillLight.position.set(-3, 1, -2);
    scene.add(fillLight);

    const rimLight = new THREE.DirectionalLight(0xB4182D, 0.4);
    rimLight.position.set(0, -3, -3);
    scene.add(rimLight);

    // ── Load Model ─────────────────────────────────────────
    const loader = new GLTFLoader();
    loader.load(
        './models/shirt.glb',
        (gltf) => {
            shirtGroup = gltf.scene;

            const box = new THREE.Box3().setFromObject(shirtGroup);
            const size = box.getSize(new THREE.Vector3());
            const center = box.getCenter(new THREE.Vector3());

            const maxDim = Math.max(size.x, size.y, size.z);
            const scaleFactor = maxDim > 0 ? 1.5 / maxDim : 1;

            shirtGroup.scale.set(scaleFactor, scaleFactor, scaleFactor);
            shirtGroup.position.set(
                -center.x * scaleFactor,
                -center.y * scaleFactor,
                -center.z * scaleFactor
            );

            shirtGroup.traverse((child) => {
                if (child.isMesh) {
                    child.material = new THREE.MeshStandardMaterial({
                        color: new THREE.Color(currentColor),
                        roughness: 0.75,
                        metalness: 0.0,
                    });
                    child.castShadow = true;
                    child.receiveShadow = true;
                }
            });

            scene.add(shirtGroup);
        },
        undefined,
        (err) => console.error('Error loading shirt model:', err)
    );

    // ── Window Resize ──────────────────────────────────────
    window.addEventListener('resize', () => {
        if (!container || !camera || !renderer) return;
        camera.aspect = container.clientWidth / container.clientHeight;
        camera.updateProjectionMatrix();
        renderer.setSize(container.clientWidth, container.clientHeight);
    });

    // ── Render Loop ────────────────────────────────────────
    renderer.setAnimationLoop(() => {
        if (controls) controls.update();
        renderer.render(scene, camera);
    });
}

// ── Decal Helper Functions ─────────────────────────────────

function createDecalMesh(texture, point, normal, shirtMesh, size) {
    const material = decalMaterial.clone();
    material.map = texture;

    // Determine orientation from normal
    const dummy = new THREE.Object3D();
    dummy.position.copy(point);
    const n = normal.clone();
    n.transformDirection(shirtMesh.matrixWorld);
    n.multiplyScalar(1); // lookAt target distance
    n.add(point);
    dummy.lookAt(n);

    // Create Decal - use a very small depth (0.15) so it doesn't punch through to the back!
    const geometry = new DecalGeometry(shirtMesh, point, dummy.rotation, new THREE.Vector3(size, size, 0.15));
    const mesh = new THREE.Mesh(geometry, material);
    scene.add(mesh);
    return mesh;
}

function updateDecalPosition(decalObj, point, normal, shirtMesh) {
    if (decalObj.mesh) {
        scene.remove(decalObj.mesh);
        decalObj.mesh.geometry.dispose();
    }
    decalObj.mesh = createDecalMesh(decalObj.texture, point, normal, shirtMesh, decalObj.size);
}

function createTextCanvasTexture(text, fontSize, fillColor, fontFamily) {
    const canvas = document.createElement('canvas');
    canvas.width = 512;
    canvas.height = 512;
    const ctx = canvas.getContext('2d');
    
    ctx.clearRect(0, 0, 512, 512);
    ctx.fillStyle = fillColor || '#000000';
    const fontStr = `bold ${fontSize || 100}px ${fontFamily || 'Outfit'}`;
    ctx.font = fontStr;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    
    // Simple word wrap
    const words = text.split(' ');
    const lines = [];
    let currentLine = words[0];
    for (let i = 1; i < words.length; i++) {
        if (ctx.measureText(currentLine + " " + words[i]).width < 480) {
            currentLine += " " + words[i];
        } else {
            lines.push(currentLine);
            currentLine = words[i];
        }
    }
    lines.push(currentLine);
    
    const lineHeight = (fontSize || 100) * 1.2;
    const totalHeight = lines.length * lineHeight;
    let startY = 256 - (totalHeight / 2) + (lineHeight / 2);
    
    lines.forEach(line => {
        ctx.fillText(line, 256, startY);
        startY += lineHeight;
    });

    const texture = new THREE.CanvasTexture(canvas);
    texture.anisotropy = renderer.capabilities.getMaxAnisotropy();
    return texture;
}

// ── Exported Blazor API ────────────────────────────────────

export function setColor(hex) {
    currentColor = hex;
    if (shirtGroup) {
        shirtGroup.traverse((child) => {
            if (child.isMesh && !decals.find(d => d.mesh === child)) {
                child.material.color.set(hex);
            }
        });
    }
}

export function setBackgroundColor(hex) {
    if (scene) {
        scene.background = new THREE.Color(hex);
    }
}

export function addText(text, fontSize, fillColor, fontFamily, originX = 0, originY = 0.2, originZ = 2, dirX = 0, dirY = 0, dirZ = -1) {
    const texture = createTextCanvasTexture(text, parseInt(fontSize)*2 || 100, fillColor, fontFamily);
    
    let shirtMesh = null;
    if (shirtGroup) {
        shirtGroup.traverse(c => { if (c.isMesh && !shirtMesh) shirtMesh = c; });
    }
    
    if (shirtMesh) {
        // Raycast from the specific origin in the specific direction
        const rc = new THREE.Raycaster();
        const origin = new THREE.Vector3(originX, originY, originZ);
        const dir = new THREE.Vector3(dirX, dirY, dirZ).normalize();
        rc.set(origin, dir);
        const intersects = rc.intersectObject(shirtMesh, false);
        
        if (intersects.length === 0) return; // Missed the shirt completely!
        
        const point = intersects[0].point;
        const normal = intersects[0].face.normal;
        
        const mesh = createDecalMesh(texture, point, normal, shirtMesh, 0.6);
        const decalObj = {
            type: 'text',
            mesh: mesh,
            texture: texture,
            size: 0.6,
            textData: { text, fontSize, fillColor, fontFamily }
        };
        decals.push(decalObj);
    }
}

export function addImage(dataUrl, size = 0.6, originX = 0, originY = 0.2, originZ = 2, dirX = 0, dirY = 0, dirZ = -1) {
    const img = new Image();
    img.src = dataUrl;
    img.onload = () => {
        const texture = new THREE.Texture(img);
        texture.needsUpdate = true;
        texture.anisotropy = renderer.capabilities.getMaxAnisotropy();
        
        let shirtMesh = null;
        if (shirtGroup) {
            shirtGroup.traverse(c => { if (c.isMesh && !shirtMesh) shirtMesh = c; });
        }
        
        if (shirtMesh) {
            const rc = new THREE.Raycaster();
            const origin = new THREE.Vector3(originX, originY, originZ);
            const dir = new THREE.Vector3(dirX, dirY, dirZ).normalize();
            rc.set(origin, dir);
            const intersects = rc.intersectObject(shirtMesh, false);
            
            if (intersects.length === 0) return;
            
            const point = intersects[0].point;
            const normal = intersects[0].face.normal;
            
            const mesh = createDecalMesh(texture, point, normal, shirtMesh, size);
            const decalObj = {
                type: 'image',
                mesh: mesh,
                texture: texture,
                size: size
            };
            decals.push(decalObj);
        }
    };
}

export async function applyDesignBatch(config) {
    // 1. Preload any images asynchronously FIRST
    const loadedItems = await Promise.all((config.items || []).map(async item => {
        if (item.type === 'image') {
            return new Promise((resolve) => {
                const img = new Image();
                img.src = item.dataUrl;
                img.onload = () => resolve({ ...item, loadedImg: img });
                img.onerror = () => resolve(null);
            });
        }
        return item;
    }));

    // 2. Everything is ready, now clear the old design! (No flickering)
    clearDesign();

    // 3. Set shirt color
    if (config.color) {
        setColor(config.color);
    }

    // 4. Apply items instantly
    let shirtMesh = null;
    if (shirtGroup) {
        shirtGroup.traverse(c => { if (c.isMesh && !shirtMesh) shirtMesh = c; });
    }

    if (shirtMesh) {
        loadedItems.filter(x => x !== null).forEach(item => {
            const rc = new THREE.Raycaster();
            const origin = new THREE.Vector3(item.originX, item.originY, item.originZ);
            const dir = new THREE.Vector3(item.dirX, item.dirY, item.dirZ).normalize();
            rc.set(origin, dir);
            const intersects = rc.intersectObject(shirtMesh, false);
            
            if (intersects.length === 0) return;
            
            const point = intersects[0].point;
            const normal = intersects[0].face.normal;
            
            let texture = null;
            if (item.type === 'text') {
                texture = createTextCanvasTexture(item.text, parseInt(item.fontSize)*2 || 100, item.color, item.font);
            } else if (item.type === 'image') {
                texture = new THREE.Texture(item.loadedImg);
                texture.needsUpdate = true;
                texture.anisotropy = renderer.capabilities.getMaxAnisotropy();
            }

            if (texture) {
                const mesh = createDecalMesh(texture, point, normal, shirtMesh, item.size || 0.6);
                decals.push({
                    type: item.type,
                    mesh: mesh,
                    texture: texture,
                    size: item.size || 0.6
                });
            }
        });
    }
}



export function clearDesign() {
    decals.forEach(d => {
        scene.remove(d.mesh);
        d.mesh.geometry.dispose();
        d.texture?.dispose();
    });
    decals.length = 0;
}

export function setCameraView(view) {
    if (!camera || !controls) return;
    const distance = 2.8;
    switch (view) {
        case 'front': camera.position.set(0, 0, distance); break;
        case 'back': camera.position.set(0, 0, -distance); break;
        case 'right': camera.position.set(distance, 0, 0); break;
        case 'left': camera.position.set(-distance, 0, 0); break;
    }
    controls.target.set(0, 0, 0);
}

export function setAutoSpin(enable) {
    if (controls) {
        controls.autoRotate = enable;
        controls.autoRotateSpeed = 2.0;
    }
}

window.downloadDataUrl = function (dataUrl, filename) {
    const a = document.createElement('a');
    a.href = dataUrl;
    a.download = filename || 'design.png';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
};
