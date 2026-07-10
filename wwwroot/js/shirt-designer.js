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
    const container = document.getElementById(containerId);
    if (!container) return;

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

    // ── Raycasting for Placement & Dragging ────────────────
    const raycaster = new THREE.Raycaster();
    const mouse = new THREE.Vector2();

    let isDraggingDecal = false;
    let isCameraDragging = false;
    const pointerDownPos = new THREE.Vector2();

    renderer.domElement.addEventListener('pointerdown', (event) => {
        pointerDownPos.set(event.clientX, event.clientY);
        isCameraDragging = false;

        const rect = renderer.domElement.getBoundingClientRect();
        mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
        mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;

        raycaster.setFromCamera(mouse, camera);

        const decalMeshes = decals.map(d => d.mesh);
        const decalIntersects = raycaster.intersectObjects(decalMeshes, false);
        
        if (decalIntersects.length > 0) {
            // Clicked a decal directly, start dragging it
            isDraggingDecal = true;
            controls.enabled = false;
            const hitMesh = decalIntersects[0].object;
            activeDecal = decals.find(d => d.mesh === hitMesh);
            
            // Hide real decal, show preview
            activeDecal.mesh.visible = false;
            dragPreviewMesh.material.map = activeDecal.texture;
            dragPreviewMesh.scale.set(activeDecal.size / 0.6, activeDecal.size / 0.6, 1);
            dragPreviewMesh.visible = true;
            
            triggerSelection();
        }
    });

    renderer.domElement.addEventListener('pointermove', (event) => {
        // Detect if the user is dragging the camera
        if (!isDraggingDecal && pointerDownPos.distanceTo(new THREE.Vector2(event.clientX, event.clientY)) > 5) {
            isCameraDragging = true;
        }

        if (isDraggingDecal && activeDecal && shirtGroup) {
            const rect = renderer.domElement.getBoundingClientRect();
            mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
            mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
            raycaster.setFromCamera(mouse, camera);

            const shirtMeshes = [];
            shirtGroup.traverse(c => { if (c.isMesh && !decals.find(d => d.mesh === c)) shirtMeshes.push(c); });
            
            const intersects = raycaster.intersectObjects(shirtMeshes, false);
            if (intersects.length > 0) {
                const hit = intersects[0];
                
                // Update preview mesh (instant)
                dragPreviewMesh.position.copy(hit.point);
                const n = hit.face.normal.clone();
                n.transformDirection(hit.object.matrixWorld);
                n.add(hit.point);
                dragPreviewMesh.lookAt(n);
                // Offset slightly along normal to prevent z-fighting
                const offset = hit.face.normal.clone().normalize().multiplyScalar(0.01);
                dragPreviewMesh.position.add(offset);
            }
        }
    });

    renderer.domElement.addEventListener('pointerup', (event) => {
        if (isDraggingDecal) {
            isDraggingDecal = false;
            controls.enabled = true;
            dragPreviewMesh.visible = false;
            
            if (activeDecal) {
                // Bake the final position
                const rect = renderer.domElement.getBoundingClientRect();
                mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
                mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
                raycaster.setFromCamera(mouse, camera);
                
                const shirtMeshes = [];
                shirtGroup.traverse(c => { if (c.isMesh && !decals.find(d => d.mesh === c)) shirtMeshes.push(c); });
                const intersects = raycaster.intersectObjects(shirtMeshes, false);
                
                if (intersects.length > 0) {
                    const hit = intersects[0];
                    updateDecalPosition(activeDecal, hit.point, hit.face.normal, hit.object);
                }
                activeDecal.mesh.visible = true;
            }
            return;
        }

        if (isCameraDragging) {
            // They were rotating the camera, don't move or deselect the decal
            return;
        }

        // It was a clean, single click (not a drag)
        const rect = renderer.domElement.getBoundingClientRect();
        mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
        mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
        raycaster.setFromCamera(mouse, camera);

        // Check if they clicked an existing decal
        const decalMeshes = decals.map(d => d.mesh);
        if (raycaster.intersectObjects(decalMeshes, false).length > 0) return; // Handled in pointerdown

        if (activeDecal && shirtGroup) {
            const shirtMeshes = [];
            shirtGroup.traverse(c => { if (c.isMesh && !decals.find(d => d.mesh === c)) shirtMeshes.push(c); });
            
            const intersects = raycaster.intersectObjects(shirtMeshes, false);
            if (intersects.length > 0) {
                // Teleport active decal to click location
                const hit = intersects[0];
                updateDecalPosition(activeDecal, hit.point, hit.face.normal, hit.object);
            } else {
                // Clicked empty space, deselect
                activeDecal = null;
                triggerSelection();
            }
        }
    });

    // ── Render Loop ────────────────────────────────────────
    renderer.setAnimationLoop(() => {
        if (controls) controls.update();
        renderer.render(scene, camera);
    });
}

function triggerSelection() {
    if (!blazorRef) return;
    if (activeDecal && activeDecal.type === 'text') {
        blazorRef.invokeMethodAsync('HandleObjectSelected', {
            type: 'text',
            text: activeDecal.textData.text,
            fontSize: activeDecal.textData.fontSize,
            fill: activeDecal.textData.fillColor,
            fontFamily: activeDecal.textData.fontFamily
        });
    } else {
        blazorRef.invokeMethodAsync('HandleObjectCleared');
    }
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

export function addText(text, fontSize, fillColor, fontFamily) {
    const texture = createTextCanvasTexture(text, parseInt(fontSize)*2 || 100, fillColor, fontFamily);
    
    let shirtMesh = null;
    if (shirtGroup) {
        shirtGroup.traverse(c => { if (c.isMesh && !shirtMesh) shirtMesh = c; });
    }
    
    if (shirtMesh) {
        // Raycast from front to hit the exact chest surface
        const rc = new THREE.Raycaster();
        rc.set(new THREE.Vector3(0, 0.2, 2), new THREE.Vector3(0, 0, -1));
        const intersects = rc.intersectObject(shirtMesh, false);
        
        const point = intersects.length > 0 ? intersects[0].point : new THREE.Vector3(0, 0.2, 0.2);
        const normal = intersects.length > 0 ? intersects[0].face.normal : new THREE.Vector3(0, 0, 1);
        
        const mesh = createDecalMesh(texture, point, normal, shirtMesh, 0.6);
        const decalObj = {
            type: 'text',
            mesh: mesh,
            texture: texture,
            size: 0.6,
            textData: { text, fontSize, fillColor, fontFamily }
        };
        decals.push(decalObj);
        activeDecal = decalObj;
        triggerSelection();
    }
}

export function updateSelectedText(properties) {
    if (activeDecal && activeDecal.type === 'text') {
        const text = properties.text !== undefined ? properties.text : activeDecal.textData.text;
        const fontSize = properties.fontSize !== undefined ? properties.fontSize : activeDecal.textData.fontSize;
        const fillColor = properties.fill !== undefined ? properties.fill : activeDecal.textData.fillColor;
        const fontFamily = properties.fontFamily !== undefined ? properties.fontFamily : activeDecal.textData.fontFamily;
        
        activeDecal.textData = { text, fontSize, fillColor, fontFamily };
        const newTexture = createTextCanvasTexture(text, parseInt(fontSize)*2 || 100, fillColor, fontFamily);
        activeDecal.texture.dispose();
        activeDecal.texture = newTexture;
        activeDecal.mesh.material.map = newTexture;
        activeDecal.mesh.material.needsUpdate = true;
    }
}

export function addImageFromDataUrl(dataUrl) {
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
            rc.set(new THREE.Vector3(0, 0.2, 2), new THREE.Vector3(0, 0, -1));
            const intersects = rc.intersectObject(shirtMesh, false);
            
            const point = intersects.length > 0 ? intersects[0].point : new THREE.Vector3(0, 0.2, 0.2);
            const normal = intersects.length > 0 ? intersects[0].face.normal : new THREE.Vector3(0, 0, 1);
            
            const mesh = createDecalMesh(texture, point, normal, shirtMesh, 0.6);
            const decalObj = {
                type: 'image',
                mesh: mesh,
                texture: texture,
                size: 0.6
            };
            decals.push(decalObj);
            activeDecal = decalObj;
            triggerSelection();
        }
    };
}

export function deleteSelectedObject() {
    if (activeDecal) {
        scene.remove(activeDecal.mesh);
        activeDecal.mesh.geometry.dispose();
        activeDecal.mesh.material?.dispose();
        activeDecal.texture?.dispose();
        
        const index = decals.indexOf(activeDecal);
        if (index > -1) decals.splice(index, 1);
        
        activeDecal = null;
        triggerSelection();
    }
}

export function deselectActiveObject() {
    activeDecal = null;
    triggerSelection();
}

export function clearCanvas() {
    decals.forEach(d => {
        scene.remove(d.mesh);
        d.mesh.geometry.dispose();
        d.texture?.dispose();
    });
    decals.length = 0;
    activeDecal = null;
    triggerSelection();
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
