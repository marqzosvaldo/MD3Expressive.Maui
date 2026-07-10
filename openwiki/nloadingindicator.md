# Ficha Técnica: NLoadingIndicator

El control `NLoadingIndicator` es un componente visual basado en **SkiaSharp** para .NET MAUI que dibuja y anima indicadores de progreso (loaders) circulares u orgánicos usando polígonos redondeados.

---

## Ciclo de Renderizado y Animación

1. **Estado Indeterminado (`Progress = -1`)**:
   El control utiliza un bucle de animación interna que interpola el progreso de rotación y morfología del polígono. Transiciona suavemente entre diferentes formas geométricas definidas en `MaterialShapes` (como un círculo de 4 esquinas a uno de 12 esquinas).
2. **Estado Determinado (`Progress` entre `0.0` y `1.0`)**:
   Dibuja una barra de progreso circular o el avance de la morfología del polígono redondeado de forma proporcional al valor asignado a la propiedad de progreso.
3. **Paso a MAUI (`CopyToMauiGeometry`)**:
   El renderizado convierte la geometría de Skia `PathF` a un control `PathGeometry` nativo de MAUI usando `OptimizedSkiaPathExtensions` para que el renderizado sea procesado por la GPU del dispositivo de forma directa.

---

## API y Miembros Públicos

### Propiedades de Enlace (BindableProperties)

* **`Progress`** (`double`):
  - Valor por defecto: `-1.0` (Modo indeterminado).
  - Rango determinado: `0.0` a `1.0`.
* **`IndicatorColor`** (`Color`):
  - Color de la línea o relleno de progreso del indicador.
  - Valor por defecto: `Colors.DeepSkyBlue`.
* **`ContainerColor`** (`Color`):
  - Color del fondo o pista del indicador.
  - Valor por defecto: `Colors.Transparent`.
* **`DeterminateTarget`** (`RoundedPolygon`):
  - Polígono redondeado que sirve como forma base para el estado determinado.
* **`IndicatorPolygons`** (`IList<RoundedPolygon>`):
  - Lista de polígonos que se utilizarán para la animación de transición de morfología en modo indeterminado.
