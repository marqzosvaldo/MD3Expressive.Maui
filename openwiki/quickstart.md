# OpenWiki: Quickstart

Bienvenido a la documentación de **MD3Expressive.Maui** generada para consumo de Agentes de IA y desarrolladores.

Este repositorio es una librería ligera de componentes visuales de alto rendimiento basada en **Material 3 Expressive** y **SkiaSharp** para .NET MAUI.

---

## Estructura del Código

El repositorio tiene la siguiente estructura simplificada:

* **[MD3Expressive.Maui.csproj](file:///Users/runner/work/MD3Expressive.Maui/MD3Expressive.Maui.csproj)**: Archivo de configuración del proyecto que compila para `net10.0`, `net10.0-android`, y `net10.0-ios`.
* **[Controls/](file:///Users/runner/work/MD3Expressive.Maui/Controls/)**:
  - `NLoadingIndicator.cs`: Control de carga nativa animado mediante SkiaSharp.
  - `NContainedLoadingIndicator.cs`: Indicador de carga alojado dentro de un contenedor con bordes redondeados.
  - `OptimizedSkiaPathExtensions.cs`: Métodos de extensión críticos para mutar las geometrías de SkiaSharp a tipos nativos de MAUI de forma directa y optimizada sin generar asignaciones de memoria (GC Churn).
* **[Graphics/Shapes/](file:///Users/runner/work/MD3Expressive.Maui/Graphics/Shapes/)**:
  - `RoundedPolygon.cs`: Clase principal para crear y redimensionar polígonos con esquinas redondeadas avanzadas de Material 3.
  - `Morph.cs`: Permite la interpolación y transición suave (morphing) entre dos polígonos redondeados diferentes.
  - `MaterialShapes.cs`: Banco de formas predeterminadas siguiendo la guía de Material 3 (corazones, flores, tréboles, estrellas, etc.).
  - `CornerRounding.cs`, `Cubic.cs`, `Point.cs`, etc.: Clases de apoyo matemático para el cálculo de curvas Bézier y redondeado de esquinas.

---

## Guía de Compilación

Para compilar el proyecto en local:
```bash
dotnet build MD3Expressive.Maui.csproj -c Release
```

---

## Documentación de Componentes
Sigue los enlaces para ver los detalles técnicos de implementación de los controles:
* [NLoadingIndicator (Ficha Técnica)](file:///Users/runner/work/MD3Expressive.Maui/openwiki/nloadingindicator.md)
* [NContainedLoadingIndicator (Ficha Técnica)](file:///Users/runner/work/MD3Expressive.Maui/openwiki/ncontainedloadingindicator.md)
