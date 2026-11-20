# Registro de Errores, Diagnósticos y Correcciones — LVL 5

> **Documento de Diagnóstico y Seguimiento Técnico**  
> **Proyecto:** EntrevistaVR (01-GestorVR) — Fundación Puerta 18  
> **Escena:** `Assets/Scenes/LVL5.unity`  
> **Última actualización:** 15/11/2026

---

## Índice de Contenidos

1. [Cambios y Correcciones Aplicadas (Ya Resueltos)](#1-cambios-y-correcciones-aplicadas-ya-resueltos)
   - [1.1 Resiliencia de Audio en `Compañero.cs`](#11-resiliencia-de-audio-en-compañerocs)
   - [1.2 Prevención de ArgumentNullException y AudioSource en `JugadorAsignadorTareas.cs`](#12-prevención-de-argumentnullexception-y-audiosource-en-jugadorasignadortareascs)
   - [1.3 Solución del Softlock de la Máquina en `GeneradorTareas.cs`](#13-solución-del-softlock-de-la-máquina-en-generadortareascs)
   - [1.4 Físicas estables y corte de subida en `HojaTarea.cs`](#14-físicas-estables-y-corte-de-subida-en-hojatareacs)
   - [1.5 Estados del Animator Controller `Esperando.controller`](#15-estados-del-animator-controller-esperandocontroller)
   - [1.6 Escena `LVL5.unity`: AudioSource de Player y Escala de Spawner](#16-escena-lvl5unity-audiosource-de-player-y-escala-de-spawner)
   - [1.7 Limpieza de archivos temporales de Blender](#17-limpieza-de-archivos-temporales-de-blender)
2. [Diagnóstico Técnico: Meta Quest 2 (Texturas e Iluminación Fallidas)](#2-diagnóstico-técnico-meta-quest-2-texturas-e-iluminación-fallidas)
   - [2.1 La Causa Raíz: Incompatibilidad entre Shaders URP y Built-in Render Pipeline](#21-la-causa-raíz-incompatibilidad-entre-shaders-urp-y-built-in-render-pipeline)
   - [2.2 Horneado de Lightmaps para Plataforma Incorrecta (PC vs Android)](#22-horneado-de-lightmaps-para-plataforma-incorrecta-pc-vs-android)
   - [2.3 Compresión de Texturas y Formatos Móviles (ASTC vs DXT)](#23-compresión-de-texturas-y-formatos-móviles-astc-vs-dxt)
   - [2.4 Tamaño excesivo de texturas (4K) en el SoC Snapdragon XR2](#24-tamaño-excesivo-de-texturas-4k-en-el-soc-snapdragon-xr2)
3. [Cómo Ver y Depurar Errores de Meta Quest SIN Contar con Visor VR](#3-cómo-ver-y-depurar-errores-de-meta-quest-sin-contar-con-visor-vr)
   - [3.1 Método 1: Cambiar de Plataforma a Android en el Editor](#31-método-1-cambiar-de-plataforma-a-android-en-el-editor)
   - [3.2 Método 2: Frame Debugger en Play Mode con XR Device Simulator](#32-método-2-frame-debugger-en-play-mode-con-xr-device-simulator)
   - [3.3 Método 3: Shader Compilation & Inspector de Warnings de GPU](#33-método-3-shader-compilation--inspector-de-warnings-de-gpu)
   - [3.4 Método 4: Development Build + Unity Profiler Autoconnect](#34-método-4-development-build--unity-profiler-autoconnect)
   - [3.5 Método 5: Captura de Logs con Android Logcat y MQDH](#35-método-5-captura-de-logs-con-android-logcat-y-mqdh)
4. [Pendientes Menores y Deuda Técnica](#4-pendientes-menores-y-deuda-técnica)

---

## 1. Cambios y Correcciones Aplicadas (Ya Resueltos)

### 1.1 Resiliencia de Audio en `Compañero.cs`
- **Problema previo:** Llamaba a `audioSource.PlayOneShot(...)` sin comprobar si `audioSource` o los clips eran nulos. En la escena los 5 compañeros tenían `audioSource: None`.
- **Corrección aplicada:**
  - Se implementó el método helper `ReproducirSonido(AudioClip clip)` con verificación de seguridad `if (audioSource != null && clip != null)`.
  - En `Start()`, se añadió auto-detección mediante `GetComponent<AudioSource>()` y `GetComponentInChildren<AudioSource>()`.
  - En `ActualizarAnimacion()`, se implementó detección dinámica de estados para mapear `"Golpeando"` como fallback de `"Enojado"` y `"boludeando"` como fallback de `"Distraido"`.
  - Se limpiaron los caracteres de codificación corruptos en el archivo C#.

### 1.2 Prevención de ArgumentNullException y AudioSource en `JugadorAsignadorTareas.cs`
- **Problema previo:** Al apuntar o soltar sin apuntar, `audioSource.PlayOneShot(null)` lanzaba `ArgumentNullException`. El componente `AudioSource` del Player además estaba apagado en el Inspector.
- **Corrección aplicada:**
  - Se implementó `ReproducirSonido(AudioClip clip)` con validación de nulos.
  - En `Start()`, auto-asigna y activa el `AudioSource` si estaba deshabilitado (`audioSource.enabled = true`).
  - Al soltar la hoja sin apuntar a un compañero, la hoja se destruye de inmediato y llama a `generador.ReintentarTarea()`, impidiendo que la máquina dispensadora quede bloqueada de por vida.

### 1.3 Solución del Softlock de la Máquina en `GeneradorTareas.cs`
- **Problema previo:** Si el jugador soltaba la tarea al vacío o se caía, `hojaActual != null` impedía generar más tareas, congelando la partida.
- **Corrección aplicada:**
  - Se añadió el método público `ReintentarTarea()` que reintegra el cupo de la tarea (`tareasRestantesPorGenerar++`), resetea `hojaActual = null` y refresca el contador de UI.
  - El archivo se reescribió en UTF-8 limpio eliminando caracteres de codificación dañados.

### 1.4 Físicas estables y corte de subida en `HojaTarea.cs`
- **Problema previo:** Si el jugador agarraba la tarea antes de que termine de subir, el script seguía forzando `transform.position += Vector3.up` en `Update()`, provocando temblores violentos (jitter) en la mano.
- **Corrección aplicada:**
  - En `OnGrab`, se marca de inmediato `salio = true;` para cortar el desplazamiento cinemático artificial.
  - Se agregó una guarda de seguridad: si la hoja cae por debajo de `Y = -2f` (fuera del mapa/suelo), avisa a `GeneradorTareas.ReintentarTarea()` y se autodestruye.

### 1.5 Estados del Animator Controller `Esperando.controller`
- **Problema previo:** Los estados en el controller eran `Esperando`, `Trabajando` y `Golpeando`. Faltaba `Distraido` y el nombre `Golpeando` no coincidía con el enum `Enojado`.
- **Corrección aplicada:**
  - Se renombró el estado `Golpeando` a `Enojado` manteniendo el clip `azul saturado (golpea la mesa).fbx`.
  - Se creó y conectó el estado `Distraido` apuntando al clip del asset `azul boludeando.fbx`.

### 1.6 Escena `LVL5.unity`: AudioSource de Player y Escala de Spawner
- **Problemas previos:**
  - `Player`: `AudioSource` tenía `m_Enabled: 0`.
  - `GeneradorTareas`: Su transform tenía escala `(0, 0, 0)`.
- **Corrección aplicada:**
  - `AudioSource` de `Player` quedó activado (`m_Enabled: 1`).
  - `GeneradorTareas` quedó restablecido a escala uniforme normal `(1, 1, 1)`.

### 1.7 Limpieza de archivos temporales de Blender
- Se trasladaron los archivos huérfanos y copias de seguridad de Blender fuera de la carpeta `Assets/` hacia `.agent/backup_assets/`:
  - `Escenario.blend1` y su `.meta`
  - `Escenario.blend.blend` y su `.meta`
  - `Escenario.blend.fbx` y su `.meta`

---

## 2. Diagnóstico Técnico: Meta Quest 2 (Texturas e Iluminación Fallidas)

### 2.1 La Causa Raíz: Incompatibilidad entre Shaders URP y Built-in Render Pipeline
El fallo donde **"no se muestra la iluminación y hay errores de textura"** en Meta Quest 2 se debe a un conflicto fundamental en la arquitectura de renderizado:

1. **Los Materiales son URP:**
   Los materiales del escenario y los compañeros (como `Artista.mat`, `Escenariofx.mat`, etc.) utilizan el shader `Universal Render Pipeline/Lit` (GUID `933532a4fcc9baf4fa0491de14d08ed7`).
2. **El Proyecto compila en Built-in Render Pipeline:**
   En `ProjectSettings/GraphicsSettings.asset` y `QualitySettings.asset` **NO hay ningún `UniversalRenderPipelineAsset` asignado**. Unity asume por defecto el renderizador clásico (Built-in).
3. **¿Qué sucede en Meta Quest 2 (Android / Vulkan / OpenGLES)?**
   - Los shaders de URP Lit contienen pases de renderizado diseñados exclusivamente para el pipeline URP (`UniversalForward`, `ShadowCaster`, `DepthOnly`).
   - En una build de Android bajo Built-in Render Pipeline, el motor busca pases como `ForwardBase` y `ForwardAdd`. Al no encontrarlos en el shader de URP:
     - El shader no puede recibir la luz direccional ni las luces puntuales/de área.
     - Los objetos se renderizan completamente oscuros/negros o planos sin relieve.
     - Las texturas de emisión, albedo o normal maps no se enlazan correctamente a los registros de Built-in, provocando pérdida de textura o superficies monocromáticas.

---

### 2.2 Horneado de Lightmaps para Plataforma Incorrecta (PC vs Android)
- **Problema:** Los mapas de iluminación horneados en `Assets/Scenes/LVL5/` (`Lightmap-0_comp_dir.png`, `LightingData.asset`) fueron generados con el Editor configurado para la plataforma **PC Standalone (Windows)**.
- **Consecuencia en Quest 2:**
  - Unity almacena los datos de bakeado por plataforma. Cuando se compila un APK para Android sin haber hecho un bake específico para Android, Unity no puede empaquetar los lightmaps de PC o descarta los pases de decodificación DXT/BC6H que el chipset Snapdragon XR2 de Quest no soporta.
  - Como resultado, la oficina en el visor se ve sin sombras horneadas ni iluminación indirecta.

---

### 2.3 Compresión de Texturas y Formatos Móviles (ASTC vs DXT)
- En Windows PC, las texturas se comprimen en formatos **DXT1 / DXT5 / BC7** compatibles con GPUs Nvidia/AMD/Intel.
- En Meta Quest 2 (chipset móvil Qualcomm Adreno 650), el estándar obligatorio para VR móvil es **ASTC** (Adaptive Scalable Texture Compression, típicamente `ASTC 4x4` o `ASTC 6x6` en balance calidad/memoria).
- Si en las opciones de importación de las texturas no está activo el tab de **Android con formato ASTC**, la GPU del Quest debe descompactar las texturas en memoria cruda RGBA32 en tiempo de carga, lo cual agota la memoria VRAM y causa pérdida de texturas o renderizado corrupto.

---

### 2.4 Tamaño excesivo de texturas (4K) en el SoC Snapdragon XR2
- En `Assets/Art/3D/Level 5/` existe la textura `floor_texture_4k.jpg` (4096 x 4096 píxeles).
- En Meta Quest 2, renderizar texturas 4K sin streaming de texturas o sin mipmaps optimizados desborda el ancho de banda de memoria del Snapdragon XR2, provocando que el driver de Vulkan/GLES descarte mips superiores, viéndose borrosa o con artefactos negros.
- En VR móvil standalone, el tamaño máximo recomendado para superficies de suelo es **2048 x 2048** o **1024 x 1024**.

---

## 3. Cómo Ver y Depurar Errores de Meta Quest SIN Contar con Visor VR

No es obligatorio tener el casco físico conectado para ver qué falla en la compilación de Meta Quest. A continuación se detallan las técnicas oficiales y profesionales:

### 3.1 Método 1: Cambiar de Plataforma a Android en el Editor
Es el paso más directo y revelador:
1. En Unity, ve a **File $\rightarrow$ Build Settings**.
2. Selecciona **Android** en la lista de plataformas.
3. Haz clic en **Switch Platform** (esto recompila la caché de assets para la GPU móvil de Android).
4. **Qué observar:**
   - Abre la pestaña **Console**.
   - Si un shader no compila para la GPU móvil (Vulkan/GLES3), aparecerán errores en rojo de tipo `Shader error in '...': program 'frag': ...`.
   - Si hay texturas incompatibles o scripts que usan librerías de Windows (`System.IO` directo, Win32, etc.), saltarán los errores inmediatamente.

---

### 3.2 Método 2: Frame Debugger en Play Mode con XR Device Simulator
El proyecto ya cuenta con el paquete **XR Device Simulator** instalado en `Assets/Samples/...`.
1. Dale al botón **Play** en el Editor de Unity.
2. Abre la ventana: **Window $\rightarrow$ Analysis $\rightarrow$ Frame Debugger**.
3. Haz clic en **Enable**.
4. **Qué te permite ver:**
   - Te desglosa la imagen frame por frame, paso a paso:
     - `RenderForwardOpaque.RenderLoopJob`
     - Qué shader se usó para dibujar cada mesa, compañero y pared.
     - Si la pasada de luz direccional (`RenderDirectionalShadows`) se ejecutó o fue omitida.
     - Si los lightmaps están bindeados en la ranura de texturas `unity_Lightmap` o si están vacíos.

---

### 3.3 Método 3: Shader Compilation & Inspector de Warnings de GPU
Para verificar si un shader fallará en Quest:
1. Selecciona en la carpeta de Assets el shader o material problemático (por ejemplo `Assets/Animations/LVL5/Artista.mat`).
2. En la parte superior del Inspector del Material, haz clic en el botón **Edit...** o selecciona el shader (`Lit`).
3. En el Inspector del Shader verás un botón llamado **Compile and show code**.
4. Haz clic en el desplegable y selecciona **Current Platform: Android (Vulkan)** o **OpenGLES3**.
5. Unity mostrará si el shader tiene errores de compilación o cuántas instrucciones consume en la GPU de Quest.

---

### 3.4 Método 4: Development Build + Unity Profiler Autoconnect
Si generas una build APK para que otra persona o tester la pruebe en un Meta Quest físico:
1. En **Build Settings**, marca las casillas:
   - `Development Build`
   - `Autoconnect Profiler`
   - `Script Debugging`
2. Cuando el tester ejecute el juego en el visor conectado a la misma red Wi-Fi de tu computadora:
3. Tu Editor de Unity abrirá la consola y recibirás **todos los `Debug.Log`, `Debug.LogError` y excepciones del visor en tu propia pantalla en tiempo real**, exactamente igual que si estuviera corriendo en tu PC.

---

### 3.5 Método 5: Captura de Logs con Android Logcat y MQDH
Si alguien te pasa el visor o si se conecta un teléfono Android/emulador para probar:
- **Meta Quest Developer Hub (MQDH):** Herramienta oficial gratuita de Meta para Windows. Tiene una pestaña de **Device Logs** donde se filtran los logs del proceso del juego con un clic.
- **Vía comando (ADB):**
  ```powershell
  adb logcat -s Unity ActivityManager PackageManager DEBUG
  ```
  Esto vuelca en la terminal todos los mensajes internos de Unity en el visor Quest, incluyendo fallos de shaders y memoria.

---

## 4. Pendientes Menores y Deuda Técnica

1. **Definir el Render Pipeline del Proyecto:**
   - Decidir si el proyecto migrará oficialmente a **Universal Render Pipeline (URP)** (creando el asset `UniversalRenderPipelineAsset` y asignándolo en `GraphicsSettings` y `QualitySettings`), o si se convertirán los materiales a **Standard (Built-in)**. *Nota: URP es altamente recomendado para Meta Quest 2 por su rendimiento mediante Single-Pass Instanced y optimización de fill-rate.*
2. **Re-bake de Iluminación en Android:**
   - Una vez definido el pipeline, cambiar la plataforma a Android y presionar **Generate Lighting** en la ventana `Window -> Rendering -> Lighting`.
3. **Optimización de Texturas Móviles:**
   - Reducir `floor_texture_4k.jpg` a max size 2048 y configurar compresión ASTC para la plataforma Android.
