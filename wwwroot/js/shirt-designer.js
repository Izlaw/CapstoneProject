import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';

let scene, camera, renderer, controls, shirt, fabricCanvas, threeTexture, animationId;
let currentColor = '#ffffff';
let isInitialized = false;
let blazorRef = null;

export async function init(containerId, fabricCanvasId, dotNetRef) {
    if (isInitialized) return;
    blazorRef = dotNetRef;

    const container = document.getElementById(containerId);
    if (!container) {
        console.error('ShirtDesigner: container not found:', containerId);
        return;
    }

    isInitialized = true;

    // ── Scene ──────────────────────────────────────────────
    scene = new THREE.Scene();

    const w = container.clientWidth || 800;
    const h = container.clientHeight || 600;

    // ── Camera ─────────────────────────────────────────────
    camera = new THREE.PerspectiveCamera(35, w / h, 0.1, 100);
    camera.position.set(0, 0, 2.8);

    // ── Renderer ───────────────────────────────────────────
    renderer = new THREE.WebGLRenderer({
        antialias: true,
        alpha: true,
        preserveDrawingBuffer: true  // required for PNG export
    });
    renderer.setSize(w, h);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure = 1.2;
    renderer.domElement.style.borderRadius = '12px';
    container.appendChild(renderer.domElement);

    // ── Orbit Controls ─────────────────────────────────────
    controls = new OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;
    controls.dampingFactor = 0.08;
    controls.minDistance = 1.8;
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

    // ── Fabric.js canvas (hidden 2D texture surface) ───────
    const canvasEl = document.getElementById(fabricCanvasId);
    fabricCanvas = new fabric.Canvas(canvasEl, {
        width: 512,
        height: 512,
        selection: true,
        renderOnAddRemove: true,
    });
    fabricCanvas.setBackgroundColor('#ffffff', fabricCanvas.renderAll.bind(fabricCanvas));

    // Use Fabric's lower canvas element as the Three.js texture source
    threeTexture = new THREE.CanvasTexture(fabricCanvas.getElement());
    threeTexture.flipY = false;
    threeTexture.needsUpdate = true;

    fabricCanvas.on('after:render', () => {
        if (threeTexture) threeTexture.needsUpdate = true;
    });

    const triggerSelection = () => {
        if (!blazorRef) return;
        const activeObj = fabricCanvas.getActiveObject();
        if (activeObj && activeObj.type === 'i-text') {
            blazorRef.invokeMethodAsync('HandleObjectSelected', {
                type: 'text',
                text: activeObj.text,
                fontSize: activeObj.fontSize,
                fill: activeObj.fill,
                fontFamily: activeObj.fontFamily
            });
        }
    };

    fabricCanvas.on('selection:created', triggerSelection);
    fabricCanvas.on('selection:updated', triggerSelection);
    fabricCanvas.on('selection:cleared', () => {
        if (blazorRef) blazorRef.invokeMethodAsync('HandleObjectCleared');
    });

    // ── Load GLB shirt model ───────────────────────────────
    const loader = new GLTFLoader();
    loader.load(
        './models/shirt.glb',
        (gltf) => {
            shirt = gltf.scene;

            // Center the model
            const box = new THREE.Box3().setFromObject(shirt);
            const center = box.getCenter(new THREE.Vector3());
            shirt.position.sub(center);

            // Apply material with canvas texture
            shirt.traverse((child) => {
                if (child.isMesh) {
                    child.material = new THREE.MeshStandardMaterial({
                        color: new THREE.Color(currentColor),
                        map: threeTexture,
                        roughness: 0.75,
                        metalness: 0.0,
                    });
                    child.castShadow = true;
                }
            });

            scene.add(shirt);
        },
        undefined,
        (err) => console.error('Error loading shirt model:', err)
    );

    // ── Animation Loop ─────────────────────────────────────
    const animate = () => {
        animationId = requestAnimationFrame(animate);
        controls.update();
        renderer.render(scene, camera);
    };
    animate();

    // ── Responsive resize ──────────────────────────────────
    const ro = new ResizeObserver(() => {
        const nw = container.clientWidth;
        const nh = container.clientHeight;
        if (nw > 0 && nh > 0) {
            camera.aspect = nw / nh;
            camera.updateProjectionMatrix();
            renderer.setSize(nw, nh);
        }
    });
    ro.observe(container);
}

// ── Public API ─────────────────────────────────────────────

export function setColor(hex) {
    currentColor = hex;
    if (!shirt) return;
    shirt.traverse((child) => {
        if (child.isMesh && child.material) {
            child.material.color.set(hex);
        }
    });
}

export function addText(text, fontSize, fillColor, fontFamily) {
    if (!fabricCanvas) return;
    const t = new fabric.IText(text, {
        left: 200,
        top: 200,
        originX: 'center',
        originY: 'center',
        fontSize: parseInt(fontSize) || 36,
        fill: fillColor || '#000000',
        fontFamily: fontFamily || 'Arial',
        fontWeight: 'bold',
        textAlign: 'center',
    });
    fabricCanvas.add(t);
    fabricCanvas.setActiveObject(t);
    fabricCanvas.renderAll();
}

export function updateSelectedText(properties) {
    if (!fabricCanvas) return;
    const activeObj = fabricCanvas.getActiveObject();
    if (activeObj && activeObj.type === 'i-text') {
        if (properties.text) activeObj.set('text', properties.text);
        if (properties.fontSize) activeObj.set('fontSize', parseInt(properties.fontSize));
        if (properties.fill) activeObj.set('fill', properties.fill);
        if (properties.fontFamily) activeObj.set('fontFamily', properties.fontFamily);
        fabricCanvas.renderAll();
    }
}

export function addImageFromDataUrl(dataUrl) {
    if (!fabricCanvas) return Promise.resolve(false);
    return new Promise((resolve) => {
        fabric.Image.fromURL(dataUrl, (img) => {
            const maxSize = 200;
            if (img.width > img.height) {
                img.scaleToWidth(maxSize);
            } else {
                img.scaleToHeight(maxSize);
            }
            img.set({ left: 200, top: 200, originX: 'center', originY: 'center' });
            fabricCanvas.add(img);
            fabricCanvas.setActiveObject(img);
            fabricCanvas.renderAll();
            resolve(true);
        }, { crossOrigin: 'anonymous' });
    });
}

export function clearDesign() {
    if (!fabricCanvas) return;
    fabricCanvas.clear();
    fabricCanvas.setBackgroundColor('#ffffff', fabricCanvas.renderAll.bind(fabricCanvas));
}

export function deleteSelected() {
    if (!fabricCanvas) return;
    const active = fabricCanvas.getActiveObject();
    if (active) {
        fabricCanvas.remove(active);
        fabricCanvas.discardActiveObject();
        fabricCanvas.renderAll();
    }
}

export function exportPng() {
    if (!renderer || !scene || !camera) return null;
    renderer.render(scene, camera);
    return renderer.domElement.toDataURL('image/png');
}

export function dispose() {
    isInitialized = false;
    if (animationId) cancelAnimationFrame(animationId);
    if (renderer) {
        renderer.dispose();
        if (renderer.domElement?.parentNode) {
            renderer.domElement.parentNode.removeChild(renderer.domElement);
        }
    }
    if (fabricCanvas) fabricCanvas.dispose();
}

// ── Global download helper (called from ExportService) ────
window.downloadDataUrl = function (dataUrl, filename) {
    const a = document.createElement('a');
    a.href = dataUrl;
    a.download = filename || 'design.png';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
};

export function setCameraView(view) {
    if (!camera || !controls) return;
    const distance = 2.8;
    switch (view) {
        case 'front':
            camera.position.set(0, 0, distance);
            break;
        case 'back':
            camera.position.set(0, 0, -distance);
            break;
        case 'right':
            camera.position.set(distance, 0, 0);
            break;
        case 'left':
            camera.position.set(-distance, 0, 0);
            break;
    }
    controls.target.set(0, 0, 0);
    controls.update();
}
