# Ficha Técnica: NContainedLoadingIndicator

El control `NContainedLoadingIndicator` es una variante de `NLoadingIndicator` que aloja el elemento de progreso animado dentro de un contenedor visual delimitado con esquinas personalizables (estilo tarjeta de progreso o loaders flotantes).

---

## Características de Diseño

* **Contenedor Orgánico**: El fondo del loader se renderiza como una superficie de color (típicamente estilo `PrimaryContainer` de Material 3) cuyas esquinas se pueden ajustar usando `CornerRadius`.
* **Esquinas Dinámicas**: Si `CornerRadius` no está definido (`null`), el contenedor se comporta como un círculo perfecto. Si se especifica un radio, se renderiza un rectángulo redondeado fluido.
* **Morfología Contenida**: El polígono indicador gira e interpola sus formas suavemente dentro del espacio asignado al contenedor, alineándose automáticamente al centro del componente.

---

## API y Miembros Públicos

### Propiedades de Enlace (BindableProperties)

* **`Progress`** (`double`):
  - Valor por defecto: `-1.0` (Modo indeterminado).
  - Rango determinado: `0.0` a `1.0`.
* **`IndicatorColor`** (`Color`):
  - Color del indicador de carga activo.
  - Valor por defecto: `#0369A1` (estilo OnPrimaryContainer).
* **`ContainerColor`** (`Color`):
  - Color del contenedor de fondo.
  - Valor por defecto: `#E0F2FE` (estilo PrimaryContainer).
* **`CornerRadius`** (`double?`):
  - Radio de curvatura del contenedor de fondo. Si es `null`, el fondo será circular.
* **`DeterminateTarget`** (`RoundedPolygon`):
  - El polígono redondeado objetivo para el estado determinado.
