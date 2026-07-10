# MD3Expressive.Maui

`MD3Expressive.Maui` es una librería de componentes de alto rendimiento para **.NET MAUI** inspirada en los lineamientos de diseño de **Material 3 Expressive**, potenciada internamente por **SkiaSharp** y optimizada para evitar la sobrecarga de recolección de basura (GC Churn).

Esta librería expone controles nativos adaptados con rendering fluido de SkiaSharp.

---

## Características Principales

* **Cero Reserva de Memoria en Animaciones**: Utiliza extensiones optimizadas para mutar geometrías in-place sin generar basura para el GC en cada cuadro.
* **Estilo Material 3 Expressive**: Formas orgánicas, esquinas redondeadas avanzadas y transformaciones de morfología dinámicas.
* **Multi-Target**: Soporte nativo para Android e iOS.

---

## Controles Disponibles

### 1. NLoadingIndicator
Un indicador de carga fluido que soporta estados tanto **indeterminados** (animación continua) como **determinados** (progreso basado en un valor de `0.0` a `1.0`), renderizado usando morfología de polígonos redondeados.

#### Ejemplo de Uso en XAML
```xml
<xmlns:controls="clr-namespace:SkiaMD3Expressive.Maui.Controls;assembly=MD3Expressive.Maui">

<controls:NLoadingIndicator 
    WidthRequest="48"
    HeightRequest="48"
    Progress="-1"
    IndicatorColor="{StaticResource PrimaryColor}"
    ContainerColor="Transparent" />
```

#### Propiedades Principales
* `Progress`: Controla el progreso. Un valor de `-1.0` activa el modo indeterminado. Valores entre `0.0` y `1.0` controlan el progreso determinado.
* `IndicatorColor`: Color de los polígonos de carga activos.
* `ContainerColor`: Color de fondo del contenedor.
* `DeterminateTarget`: El `RoundedPolygon` objetivo para el estado determinado.

---

### 2. NContainedLoadingIndicator
Una variante del indicador de carga que vive dentro de un contenedor visualmente delimitado con esquinas redondeadas personalizables, ideal para diálogos de progreso y loaders sobrepuestos.

#### Ejemplo de Uso en XAML
```xml
<controls:NContainedLoadingIndicator
    WidthRequest="80"
    HeightRequest="80"
    Progress="-1"
    CornerRadius="16"
    IndicatorColor="#0369A1"
    ContainerColor="#E0F2FE" />
```

#### Propiedades Principales
* `CornerRadius`: Define el radio de las esquinas del contenedor. Si es `null`, se renderiza como un contenedor completamente circular.
* `ContainerColor`: El color del contenedor de fondo (por defecto estilo PrimaryContainer).
* `IndicatorColor`: El color de la barra o polígono de progreso (por defecto estilo OnPrimaryContainer).

---

## Instalación

1. Agrega el paquete a tu proyecto de .NET MAUI:
   ```bash
   dotnet add package MD3Expressive.Maui
   ```
2. Inicializa los controles en tu `MauiProgram.cs` si utilizas dependencias adicionales de SkiaSharp:
   ```csharp
   using SkiaSharp.Views.Maui.Controls.Hosting;

   public static class MauiProgram
   {
       public static MauiApp CreateMauiApp()
       {
           var builder = MauiApp.CreateBuilder();
           builder
               .UseMauiApp<App>()
               .UseSkiaSharp(); // Requerido para el motor gráfico de Skia
           
           return builder.Build();
       }
   }
   ```
