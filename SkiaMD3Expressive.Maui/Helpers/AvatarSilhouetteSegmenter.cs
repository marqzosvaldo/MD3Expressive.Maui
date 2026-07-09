using System;
using System.IO;
using System.Threading.Tasks;
using SkiaSharp;

#if IOS || MACCATALYST
using Vision;
using Foundation;
using CoreGraphics;
using CoreVideo;
using SkiaSharp.Views.iOS;
using UIKit;
#elif ANDROID
using Android.Runtime;
using Java.Lang;
using Java.Nio;
#endif

namespace SkiaMD3Expressive.Maui.Helpers
{
    public static class AvatarSilhouetteSegmenter
    {
        private static readonly System.Threading.SemaphoreSlim _segmentationSemaphore = new System.Threading.SemaphoreSlim(1, 1);

        private static void WriteLog(string fileName, string text)
        {
            try
            {
                var path = Path.Combine(Microsoft.Maui.Storage.FileSystem.CacheDirectory, fileName);
                File.WriteAllText(path, $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\n{text}\n\n");
            }
            catch { }
#if ANDROID
            Android.Util.Log.Info("AvatarSilhouetteSegmenter", text);
#endif
            System.Diagnostics.Debug.WriteLine($"[AvatarSilhouetteSegmenter] {text}");
        }

        public static SKBitmap ScaleBitmapDown(SKBitmap original, int maxDim)
        {
            if (original.Width <= maxDim && original.Height <= maxDim)
            {
                return original;
            }

            float ratio = (float)original.Width / original.Height;
            int newWidth, newHeight;
            if (ratio > 1f)
            {
                newWidth = maxDim;
                newHeight = (int)(maxDim / ratio);
            }
            else
            {
                newHeight = maxDim;
                newWidth = (int)(maxDim * ratio);
            }

            var scaled = new SKBitmap(newWidth, newHeight, original.ColorType, original.AlphaType);
            original.ScalePixels(scaled, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
            return scaled;
        }

        public static async Task<SKBitmap?> SegmentSelfieAsync(SKBitmap originalBitmap)
        {
            if (originalBitmap == null || originalBitmap.Width <= 0 || originalBitmap.Height <= 0)
            {
                WriteLog("segmenter_error.txt", "SegmentSelfieAsync: originalBitmap is null or invalid dimensions");
                return null;
            }

            WriteLog("segmenter_status.txt", $"SegmentSelfieAsync Start. Size: {originalBitmap.Width}x{originalBitmap.Height}");

            await _segmentationSemaphore.WaitAsync();
            try
            {
#if IOS || MACCATALYST
                return SegmentSelfieIos(originalBitmap);
#elif ANDROID
                return await SegmentSelfieAndroid(originalBitmap);
#else
                WriteLog("segmenter_error.txt", "SegmentSelfieAsync: Unsupported platform");
                return null;
#endif
            }
            catch (System.Exception ex)
            {
                var errorMsg = $"[SegmentSelfieAsync Catch]\nMessage: {ex.Message}\nStackTrace:\n{ex.StackTrace}";
                System.Diagnostics.Debug.WriteLine($"[AvatarSilhouetteSegmenter] Error in SegmentSelfieAsync: {ex.Message}");
                WriteLog("segmenter_error.txt", errorMsg);
                return null;
            }
            finally
            {
                _segmentationSemaphore.Release();
            }
        }

#if IOS || MACCATALYST
        private static SKBitmap? SegmentSelfieIos(SKBitmap originalBitmap)
        {
            SKBitmap? scaledBitmap = null;
            UIImage? uiImage = null;
            CGImage? cgImage = null;
            try
            {
                // Downscale image to a max dimension of 512 for performance, resource bounds, and ML consistency (preserves 4x more detail than 256)
                scaledBitmap = ScaleBitmapDown(originalBitmap, 512);

                // Diagnose scaledBitmap pixel values to verify it is loaded and has color content
                try
                {
                    int sBmpW = scaledBitmap.Width;
                    int sBmpH = scaledBitmap.Height;
                    var sBmpColor = scaledBitmap.ColorType;
                    var sBmpAlpha = scaledBitmap.AlphaType;
                    int bytesPerPixel = scaledBitmap.BytesPerPixel;
                    int rowBytes = scaledBitmap.RowBytes;
                    IntPtr pixelsAddr = scaledBitmap.GetPixels();

                    if (pixelsAddr != IntPtr.Zero && bytesPerPixel > 0)
                    {
                        int bMinR = 255, bMaxR = 0;
                        long bSumR = 0;
                        int bMinG = 255, bMaxG = 0;
                        long bSumG = 0;
                        int bMinB = 255, bMaxB = 0;
                        long bSumB = 0;
                        int bMinA = 255, bMaxA = 0;
                        long bSumA = 0;
                        
                        long totalPixels = (long)sBmpW * sBmpH;

                        unsafe
                        {
                            byte* basePtr = (byte*)pixelsAddr.ToPointer();
                            
                            // Check if color format is 4-byte (RGBA, BGRA, etc.)
                            if (bytesPerPixel == 4)
                            {
                                for (int y = 0; y < sBmpH; y++)
                                {
                                    byte* rowPtr = basePtr + (y * rowBytes);
                                    for (int x = 0; x < sBmpW; x++)
                                    {
                                        byte* pixel = rowPtr + (x * 4);
                                        byte r = 0, g = 0, b = 0, a = 0;
                                        if (sBmpColor == SKColorType.Rgba8888)
                                        {
                                            r = pixel[0];
                                            g = pixel[1];
                                            b = pixel[2];
                                            a = pixel[3];
                                        }
                                        else if (sBmpColor == SKColorType.Bgra8888)
                                        {
                                            b = pixel[0];
                                            g = pixel[1];
                                            r = pixel[2];
                                            a = pixel[3];
                                        }
                                        else
                                        {
                                            // Fallback default mapping
                                            r = pixel[0];
                                            g = pixel[1];
                                            b = pixel[2];
                                            a = pixel[3];
                                        }

                                        if (r < bMinR) bMinR = r;
                                        if (r > bMaxR) bMaxR = r;
                                        bSumR += r;

                                        if (g < bMinG) bMinG = g;
                                        if (g > bMaxG) bMaxG = g;
                                        bSumG += g;

                                        if (b < bMinB) bMinB = b;
                                        if (b > bMaxB) bMaxB = b;
                                        bSumB += b;

                                        if (a < bMinA) bMinA = a;
                                        if (a > bMaxA) bMaxA = a;
                                        bSumA += a;
                                    }
                                }
                                float avgR = (float)bSumR / totalPixels;
                                float avgG = (float)bSumG / totalPixels;
                                float avgB = (float)bSumB / totalPixels;
                                float avgA = (float)bSumA / totalPixels;

                                var bmpLog = $"[AvatarSilhouetteSegmenter] scaledBitmap: {sBmpW}x{sBmpH}, Color: {sBmpColor}, Alpha: {sBmpAlpha}\n" +
                                             $"[AvatarSilhouetteSegmenter]   Red: Min={bMinR}, Max={bMaxR}, Avg={avgR}\n" +
                                             $"[AvatarSilhouetteSegmenter]   Green: Min={bMinG}, Max={bMaxG}, Avg={avgG}\n" +
                                             $"[AvatarSilhouetteSegmenter]   Blue: Min={bMinB}, Max={bMaxB}, Avg={avgB}\n" +
                                             $"[AvatarSilhouetteSegmenter]   Alpha: Min={bMinA}, Max={bMaxA}, Avg={avgA}";
                                System.Diagnostics.Debug.WriteLine(bmpLog);
                                WriteLog("segmenter_status.txt", bmpLog);
                            }
                            else
                            {
                                var bmpLog = $"[AvatarSilhouetteSegmenter] scaledBitmap: {sBmpW}x{sBmpH}, Color: {sBmpColor}, Alpha: {sBmpAlpha}, BytesPerPixel: {bytesPerPixel} (Unsupported for detailed diagnostics)";
                                System.Diagnostics.Debug.WriteLine(bmpLog);
                                WriteLog("segmenter_status.txt", bmpLog);
                            }
                        }
                    }
                    else
                    {
                        var bmpLog = $"[AvatarSilhouetteSegmenter] scaledBitmap has no pixels! Width={sBmpW}, Height={sBmpH}";
                        System.Diagnostics.Debug.WriteLine(bmpLog);
                        WriteLog("segmenter_status.txt", bmpLog);
                    }
                }
                catch (System.Exception ex)
                {
                    var errorMsg = $"[AvatarSilhouetteSegmenter] Error diagnosing input bitmap: {ex.Message}\n{ex.StackTrace}";
                    System.Diagnostics.Debug.WriteLine(errorMsg);
                    WriteLog("segmenter_status.txt", errorMsg);
                }

                uiImage = scaledBitmap.ToUIImage();
                if (uiImage == null)
                {
                    WriteLog("segmenter_error.txt", "SegmentSelfieIos: ToUIImage returned null");
                    return null;
                }
                cgImage = uiImage.CGImage;
                if (cgImage == null)
                {
                    var cgNullMsg = "[AvatarSilhouetteSegmenter] Warning: uiImage.CGImage is null!";
                    System.Diagnostics.Debug.WriteLine(cgNullMsg);
                    WriteLog("segmenter_status.txt", cgNullMsg);
                }
                else
                {
                    var cgOkMsg = $"[AvatarSilhouetteSegmenter] uiImage.CGImage is valid: {cgImage.Width}x{cgImage.Height}";
                    System.Diagnostics.Debug.WriteLine(cgOkMsg);
                    WriteLog("segmenter_status.txt", cgOkMsg);
                }

                using var request = new VNGeneratePersonSegmentationRequest
                {
                    QualityLevel = VNGeneratePersonSegmentationRequestQualityLevel.Balanced,
                    OutputPixelFormat = (uint)CVPixelFormatType.OneComponent8
                };

                // Fallback to CPU execution if running on a simulator to avoid context allocation failures (CoreML ANE/GPU limits)
                if (Microsoft.Maui.Devices.DeviceInfo.Current.DeviceType == Microsoft.Maui.Devices.DeviceType.Virtual)
                {
                    request.UsesCpuOnly = true;
                }

                using var handler = new VNImageRequestHandler(cgImage, new VNImageOptions());
                var success = handler.Perform(new[] { request }, out var error);
                if (!success || error != null)
                {
                    var errorDetails = $"Perform failed. Success: {success}, Error: {error?.LocalizedDescription}";
                    System.Diagnostics.Debug.WriteLine($"[AvatarSilhouetteSegmenter] iOS Vision Perform failed: {errorDetails}");
                    WriteLog("segmenter_error.txt", $"SegmentSelfieIos: {errorDetails}");
                    return null;
                }

                var results = request.GetResults<VNPixelBufferObservation>();
                if (results == null || results.Length == 0)
                {
                    WriteLog("segmenter_error.txt", $"SegmentSelfieIos: results is null or empty. Results count: {(results == null ? "null" : results.Length.ToString())}");
                    return null;
                }

                var observation = results[0];
                using var pixelBuffer = observation.PixelBuffer;
                if (pixelBuffer == null)
                {
                    WriteLog("segmenter_error.txt", "SegmentSelfieIos: PixelBuffer is null");
                    return null;
                }

                pixelBuffer.Lock(CVPixelBufferLock.ReadOnly);
                try
                {
                    int width = (int)pixelBuffer.Width;
                    int height = (int)pixelBuffer.Height;
                    IntPtr baseAddress = pixelBuffer.BaseAddress;
                    int bytesPerRow = (int)pixelBuffer.BytesPerRow;
                    var format = pixelBuffer.PixelFormatType;
                    bool isPlanar = pixelBuffer.IsPlanar;

                    var bufLog = $"[AvatarSilhouetteSegmenter] iOS Vision returned mask: {width}x{height}, format: {format}, bytesPerRow: {bytesPerRow}, isPlanar: {isPlanar}";
                    System.Diagnostics.Debug.WriteLine(bufLog);
                    WriteLog("segmenter_status.txt", bufLog);

                    // Create Alpha8 bitmap for the mask
                    var maskBitmap = new SKBitmap(width, height, SKColorType.Alpha8, SKAlphaType.Premul);
                    IntPtr destAddress = maskBitmap.GetPixels();
                    int destRowBytes = maskBitmap.RowBytes;

                    unsafe
                    {
                        byte* srcPtr = (byte*)baseAddress.ToPointer();
                        byte* destPtr = (byte*)destAddress.ToPointer();

                        if (format == CVPixelFormatType.OneComponent8)
                        {
                            for (int y = 0; y < height; y++)
                            {
                                System.Buffer.MemoryCopy(srcPtr + (y * bytesPerRow), destPtr + (y * destRowBytes), width, width);
                            }
                        }
                        else if (format == CVPixelFormatType.OneComponent32Float)
                        {
                            for (int y = 0; y < height; y++)
                            {
                                float* srcRow = (float*)(srcPtr + (y * bytesPerRow));
                                byte* destRow = destPtr + (y * destRowBytes);
                                for (int x = 0; x < width; x++)
                                {
                                    float confidence = srcRow[x];
                                    destRow[x] = (byte)(System.Math.Clamp(confidence, 0.0f, 1.0f) * 255f);
                                }
                            }
                        }
                        else if (format == CVPixelFormatType.OneComponent16Half)
                        {
                            for (int y = 0; y < height; y++)
                            {
                                System.Half* srcRow = (System.Half*)(srcPtr + (y * bytesPerRow));
                                byte* destRow = destPtr + (y * destRowBytes);
                                for (int x = 0; x < width; x++)
                                {
                                    float confidence = (float)srcRow[x];
                                    destRow[x] = (byte)(System.Math.Clamp(confidence, 0.0f, 1.0f) * 255f);
                                }
                            }
                        }
                        else
                        {
                            for (int y = 0; y < height; y++)
                            {
                                System.Buffer.MemoryCopy(srcPtr + (y * bytesPerRow), destPtr + (y * destRowBytes), width, width);
                            }
                        }
                    }

                    // Print mask statistics before scaling
                    byte[] debugBytes = new byte[width * height];
                    System.Runtime.InteropServices.Marshal.Copy(destAddress, debugBytes, 0, debugBytes.Length);
                    int minVal = 255, maxVal = 0, sumVal = 0;
                    for (int i = 0; i < debugBytes.Length; i++)
                    {
                        byte val = debugBytes[i];
                        if (val < minVal) minVal = val;
                        if (val > maxVal) maxVal = val;
                        sumVal += val;
                    }
                    float avgVal = (float)sumVal / debugBytes.Length;
                    System.Diagnostics.Debug.WriteLine($"[AvatarSilhouetteSegmenter] maskBitmap - Min: {minVal}, Max: {maxVal}, Avg: {avgVal}");
                    WriteLog("segmenter_status.txt", $"SegmentSelfieIos maskBitmap stats - Min: {minVal}, Max: {maxVal}, Avg: {avgVal}");

                    // Graceful Fallback: If max confidence in mask is extremely low (< 10),
                    // the model failed to detect a person (typical on iOS Simulator CPU execution).
                    // We return null to fallback to the default circular vignette layout instead of drawing a blank avatar.
                    if (maxVal < 10)
                    {
                        var failLog = $"[AvatarSilhouetteSegmenter] Person detection failed (Max value in mask: {maxVal}). Falling back to circular layout.";
                        System.Diagnostics.Debug.WriteLine(failLog);
                        WriteLog("segmenter_status.txt", failLog);
                        
                        maskBitmap.Dispose();
                        return null;
                    }

                    // Scale mask to original bitmap size
                    var scaledMask = new SKBitmap(originalBitmap.Width, originalBitmap.Height, SKColorType.Alpha8, SKAlphaType.Premul);
                    maskBitmap.ScalePixels(scaledMask, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));

                    // Print scaledMask statistics after scaling
                    byte[] scaledDebugBytes = new byte[originalBitmap.Width * originalBitmap.Height];
                    System.Runtime.InteropServices.Marshal.Copy(scaledMask.GetPixels(), scaledDebugBytes, 0, scaledDebugBytes.Length);
                    int sMin = 255, sMax = 0, sSum = 0;
                    for (int i = 0; i < scaledDebugBytes.Length; i++)
                    {
                        byte val = scaledDebugBytes[i];
                        if (val < sMin) sMin = val;
                        if (val > sMax) sMax = val;
                        sSum += val;
                    }
                    float sAvg = (float)sSum / scaledDebugBytes.Length;
                    System.Diagnostics.Debug.WriteLine($"[AvatarSilhouetteSegmenter] scaledMask - Min: {sMin}, Max: {sMax}, Avg: {sAvg}");
                    WriteLog("segmenter_status.txt", $"SegmentSelfieIos scaledMask stats - Min: {sMin}, Max: {sMax}, Avg: {sAvg}");

                    maskBitmap.Dispose();
                    return scaledMask;
                }
                finally
                {
                    pixelBuffer.Unlock(CVPixelBufferLock.ReadOnly);
                }
            }
            catch (System.Exception ex)
            {
                var errorMsg = $"[SegmentSelfieIos Catch]\nMessage: {ex.Message}\nStackTrace:\n{ex.StackTrace}";
                System.Diagnostics.Debug.WriteLine($"[AvatarSilhouetteSegmenter] iOS Segmenter Error: {ex.Message}");
                WriteLog("segmenter_error.txt", errorMsg);
                return null;
            }
            finally
            {
                if (scaledBitmap != null && scaledBitmap != originalBitmap)
                {
                    scaledBitmap.Dispose();
                }
                uiImage?.Dispose();
                cgImage?.Dispose();
            }
        }
#endif

#if ANDROID
        private static Java.Lang.Class LoadClass(string className)
        {
            return Java.Lang.Class.ForName(className, true, Android.App.Application.Context.ClassLoader);
        }

        private static async Task<SKBitmap?> SegmentSelfieAndroid(SKBitmap originalBitmap)
        {
            SKBitmap? scaledBitmap = null;
            Android.Graphics.Bitmap? androidBitmap = null;
            Android.Graphics.Bitmap? tempAndroidBitmap = null;
            SKBitmap? tempBitmap = null;
            Java.Nio.ByteBuffer? byteBuffer = null;

            try
            {
                WriteLog("segmenter_status.txt", "SegmentSelfieAndroid: Downscaling original bitmap...");
                // Downscale the original bitmap to a maximum dimension of 256 to prevent hangs/OOM on emulators
                scaledBitmap = ScaleBitmapDown(originalBitmap, 256);
                WriteLog("segmenter_status.txt", $"SegmentSelfieAndroid: Downscaled to {scaledBitmap.Width}x{scaledBitmap.Height}");

                // Ensure image is in Rgba8888 color format to match Android ARGB_8888 config
                tempBitmap = new SKBitmap(scaledBitmap.Width, scaledBitmap.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                using (var canvas = new SKCanvas(tempBitmap))
                {
                    canvas.DrawBitmap(scaledBitmap, 0, 0);
                }

                // Copy to Android native bitmap
                androidBitmap = Android.Graphics.Bitmap.CreateBitmap(tempBitmap.Width, tempBitmap.Height, Android.Graphics.Bitmap.Config.Argb8888!);
                byteBuffer = Java.Nio.ByteBuffer.Wrap(tempBitmap.Bytes);
                androidBitmap.CopyPixelsFromBuffer(byteBuffer);

                WriteLog("segmenter_status.txt", $"SegmentSelfieAndroid: Created native bitmaps, loading JNI classes...");

                // Load ML Kit Selfie Segmenter via JNI using application ClassLoader
                using var optionsBuilderClass = LoadClass("com.google.mlkit.vision.segmentation.selfie.SelfieSegmenterOptions$Builder");
                using var builderConstructor = optionsBuilderClass.GetConstructor(Array.Empty<Java.Lang.Class>());
                using var builderInstance = builderConstructor.NewInstance(Array.Empty<Java.Lang.Object>());

                // Set to Single Image Mode (value = 2)
                using var setDetectorModeMethod = optionsBuilderClass.GetMethod("setDetectorMode", new Java.Lang.Class[] { Java.Lang.Integer.Type });
                setDetectorModeMethod.Invoke(builderInstance, new Java.Lang.Object[] { new Java.Lang.Integer(2) });

                // Build options
                using var buildMethod = optionsBuilderClass.GetMethod("build", Array.Empty<Java.Lang.Class>());
                using var options = buildMethod.Invoke(builderInstance, Array.Empty<Java.Lang.Object>());
                WriteLog("segmenter_status.txt", "SegmentSelfieAndroid: Built SelfieSegmenterOptions");

                // Get client segmenter
                using var segmentationClass = LoadClass("com.google.mlkit.vision.segmentation.Segmentation");
                using var getClientMethod = segmentationClass.GetMethod("getClient", new Java.Lang.Class[] { LoadClass("com.google.mlkit.vision.segmentation.selfie.SelfieSegmenterOptions") });
                using var segmenter = getClientMethod.Invoke(null, new Java.Lang.Object[] { options });
                WriteLog("segmenter_status.txt", "SegmentSelfieAndroid: Obtained segmenter client");

                // Convert Android Bitmap to InputImage: InputImage.fromBitmap(bitmap, 0)
                using var inputImageClass = LoadClass("com.google.mlkit.vision.common.InputImage");
                using var fromBitmapMethod = inputImageClass.GetMethod("fromBitmap", new Java.Lang.Class[] { 
                    LoadClass("android.graphics.Bitmap"), 
                    Java.Lang.Integer.Type 
                });
                using var inputImage = fromBitmapMethod.Invoke(null, new Java.Lang.Object[] { androidBitmap, new Java.Lang.Integer(0) });
                WriteLog("segmenter_status.txt", "SegmentSelfieAndroid: Created InputImage from Android Bitmap");

                // Run process() using public Segmenter class method signature to avoid package-private JNI restrictions
                using var segmenterClass = LoadClass("com.google.mlkit.vision.segmentation.Segmenter");
                using var processMethod = segmenterClass.GetMethod("process", new Java.Lang.Class[] { LoadClass("com.google.mlkit.vision.common.InputImage") });
                WriteLog("segmenter_status.txt", "SegmentSelfieAndroid: Invoking process() method...");
                using var taskObject = processMethod.Invoke(segmenter, new Java.Lang.Object[] { inputImage });

                // Wait for task completion using Tasks.await(taskObject, 5, SECONDS) to prevent infinite hangs
                using var tasksClass = LoadClass("com.google.android.gms.tasks.Tasks");
                using var timeUnitClass = LoadClass("java.util.concurrent.TimeUnit");
                using var secondsField = timeUnitClass.GetField("SECONDS");
                using var secondsUnit = secondsField.Get(null);

                using var awaitMethod = tasksClass.GetMethod("await", new Java.Lang.Class[] { 
                    LoadClass("com.google.android.gms.tasks.Task"), 
                    Java.Lang.Long.Type, 
                    timeUnitClass 
                });

                WriteLog("segmenter_status.txt", "SegmentSelfieAndroid: Awaiting task completion via Tasks.await with 5s timeout...");

                using var resultObject = awaitMethod.Invoke(null, new Java.Lang.Object[] { 
                    taskObject, 
                    new Java.Lang.Long(5), 
                    secondsUnit 
                });
                WriteLog("segmenter_status.txt", "SegmentSelfieAndroid: Tasks.await completed, extracting mask data...");

                // Extract mask properties from the public SegmentationMask class to avoid JNI accessibility restrictions
                using var maskClass = LoadClass("com.google.mlkit.vision.segmentation.SegmentationMask");
                using var getBufferMethod = maskClass.GetMethod("getBuffer", Array.Empty<Java.Lang.Class>());
                using var getWidthMethod = maskClass.GetMethod("getWidth", Array.Empty<Java.Lang.Class>());
                using var getHeightMethod = maskClass.GetMethod("getHeight", Array.Empty<Java.Lang.Class>());

                using var javaBuffer = (Java.Nio.ByteBuffer)getBufferMethod.Invoke(resultObject, Array.Empty<Java.Lang.Object>());
                int maskWidth = (int)getWidthMethod.Invoke(resultObject, Array.Empty<Java.Lang.Object>());
                int maskHeight = (int)getHeightMethod.Invoke(resultObject, Array.Empty<Java.Lang.Object>());

                if (javaBuffer == null || maskWidth <= 0 || maskHeight <= 0)
                {
                    WriteLog("segmenter_error.txt", $"SegmentSelfieAndroid: Invalid mask buffer or dimensions: {maskWidth}x{maskHeight}");
                    return null;
                }
                WriteLog("segmenter_status.txt", $"SegmentSelfieAndroid: Mask buffer successfully loaded: {maskWidth}x{maskHeight}");

                javaBuffer.Rewind();

                // Mask consists of 32-bit floats (confidence from 0.0 to 1.0)
                int pixelCount = maskWidth * maskHeight;
                float[] floatValues = new float[pixelCount];

                using (var floatBuffer = javaBuffer.AsFloatBuffer())
                {
                    floatBuffer.Get(floatValues);
                }

                // Analyze confidence statistics
                float minConf = 1.0f, maxConf = 0.0f, sumConf = 0.0f;
                for (int i = 0; i < pixelCount; i++)
                {
                    float confidence = floatValues[i];
                    if (confidence < minConf) minConf = confidence;
                    if (confidence > maxConf) maxConf = confidence;
                    sumConf += confidence;
                }
                float avgConf = sumConf / pixelCount;
                WriteLog("segmenter_status.txt", $"SegmentSelfieAndroid Confidence Stats - Min: {minConf:F4}, Max: {maxConf:F4}, Avg: {avgConf:F4}");

                // Convert float confidence values to alpha bytes
                byte[] alphaBytes = new byte[pixelCount];
                for (int i = 0; i < pixelCount; i++)
                {
                    float confidence = floatValues[i];
                    alphaBytes[i] = (byte)(System.Math.Clamp(confidence, 0.0f, 1.0f) * 255f);
                }

                // Create mask SKBitmap
                var maskBitmap = new SKBitmap(maskWidth, maskHeight, SKColorType.Alpha8, SKAlphaType.Premul);
                System.Runtime.InteropServices.Marshal.Copy(alphaBytes, 0, maskBitmap.GetPixels(), pixelCount);

                // Scale to original size
                var scaledMask = new SKBitmap(originalBitmap.Width, originalBitmap.Height, SKColorType.Alpha8, SKAlphaType.Premul);
                maskBitmap.ScalePixels(scaledMask, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));

                maskBitmap.Dispose();
                WriteLog("segmenter_status.txt", $"SegmentSelfieAndroid Success. Mask scaled to {originalBitmap.Width}x{originalBitmap.Height}");
                return scaledMask;
            }
            catch (System.Exception ex)
            {
                var errorMsg = $"[SegmentSelfieAndroid Catch]\nMessage: {ex.Message}\nStackTrace:\n{ex.StackTrace}\nFullException: {ex}";
                Android.Util.Log.Error("AvatarSilhouetteSegmenter", $"Android Segmenter Error: {ex}");
                WriteLog("segmenter_error.txt", errorMsg);
                return null;
            }
            finally
            {
                if (scaledBitmap != null && scaledBitmap != originalBitmap)
                {
                    scaledBitmap.Dispose();
                }
                tempBitmap?.Dispose();
                androidBitmap?.Dispose();
                tempAndroidBitmap?.Dispose();
                byteBuffer?.Dispose();
            }
        }
#endif
    }
}

