# Evaluación RTF → HTML

Fecha: 2026-09-29. Windows, SDK .NET 9.0.304. Ensayo técnico, no ejecutable de producción.

## Conclusión

Una biblioteca integrada es viable sin Word ni LibreOffice. RtfPipe es la primera opción si se prioriza no pagar licencias, pero necesita conversión WMF → PNG y resolver diferencias de alineación antes de aprobar contratos. Aspose.Words comercial es la alternativa con mejor resultado observado en imágenes y alineación del bloque del cliente; requiere licencia y tampoco queda certificada su fidelidad de impresión con esta prueba.

No recomiendo aprobar ninguna como conversor de fidelidad idéntica todavía. Convertir sin excepción no demuestra que el documento se imprima igual.

## Candidatos

- RtfPipe 2.0.7677.4303: licencia MIT. Probado con los seis RTF. Su documentación reconoce limitaciones visuales en documentos complejos. Debe conservarse el aviso de licencia al distribuir.
- Aspose.Words 26.9.0: probado sin licencia, en modo evaluación. Introduce mensajes y una imagen de marca de agua. La evaluación limita el tamaño del documento; no equivale a una licencia de producción. No se han eliminado sus marcas.
- Aspose.Words FOSS: descartado por documentación, sin ensayo: no admite cargar RTF ni exportar HTML.

Fuentes oficiales:
- https://github.com/erdomke/RtfPipe
- https://github.com/erdomke/RtfPipe/blob/master/LICENSE
- https://docs.aspose.com/words/net/licensing/
- https://kb.aspose.org/words/net/faq/

## Resultados reproducibles

| Archivo | Tablas RtfPipe / Aspose | Filas RtfPipe / Aspose | Imágenes originales | PDF Edge, páginas RtfPipe / Aspose |
|---|---:|---:|---:|---:|
| Polecenie wymiany wodomierza | 2 / 2 | 12 / 12 | 0 | 1 / 1 |
| Potwierdzenie salda | 1 / 1 | 5 / 5 | 0 | 1 / 1 |
| Sobótka | 2 / 2 | 23 / 24 | 0 | 2 / 2 |
| Sobótka_1 | 3 / 3 | 7 / 7 | 1 WMF | 1 / 1 |
| Sobótka_2 | 3 / 3 | 64 / 65 | 2 WMF | 3 / 3 |
| Sobótka_3 | 4 / 4 | 36 / 37 | 0 | 1 / 1 |

Los recuentos describen el HTML, no certifican equivalencia estructural. Aspose puede crear filas adicionales de representación. Los PDF son impresiones del HTML en Edge (A4 solicitado, respetando CSS de página); no son referencias del RTF ni comparaciones contra WordPad. Las marcas de evaluación de Aspose afectan al resultado.

Ambos motores:
- 6/6 conversiones sin excepciones.
- Misma secuencia de marcadores entre ambos motores en cada archivo, tras extraer texto del HTML.
- Mismos recuentos de caracteres polacos entre ambos, sin U+FFFD. Esto por sí solo no demuestra ausencia de errores de codificación.
- Salidas HTML autosuficientes en cuanto a imágenes, sin aplicaciones ofimáticas.

RtfPipe:
- Conserva las 147 filas indicadas por las muestras RTF y produce 15 tablas en total.
- Sus tres imágenes WMF no cargan en Edge: 1 en Sobótka_1 y 2 en Sobótka_2.
- La prueba adicional RtfPipePng rasteriza WMF con System.Drawing/GDI+ de Windows y las tres cargan. Es una prueba de viabilidad; dimensiones, recorte y resolución de impresión requieren ajuste de producción.
- Comparación independiente con texto de RichTextBox de Windows: igualdad ignorando espacios en Polecenie, Potwierdzenie y Sobótka. En los otros tres hay diferencias iniciales de símbolos Wingdings; no se afirma igualdad completa. Véase text-comparison.json.
- Revisión visual de Sobótka_1: bloque de cliente a la izquierda, a diferencia de Aspose y del HTML LibreOffice suministrado. También cambian espacios verticales respecto al HTML de referencia. No se ha demostrado cuál reproduce exactamente el original RTF.
- Revisión visual de Polecenie: las dos tablas están presentes; los marcadores largos se parten visualmente dentro de celdas estrechas.

Aspose:
- Convierte las tres imágenes originales a PNG visibles en Edge.
- Añade una imagen de evaluación por documento; no debe confundirse con imágenes originales.
- En Sobótka_1 conserva la ubicación a la derecha del bloque del cliente, coincidente con la referencia HTML suministrada.
- Se aprecia texto y marca de agua de evaluación. Para una comparación limpia hace falta licencia válida o temporal facilitada por el proveedor.

## Artefactos

- LibraryEvaluation/: código y versiones NuGet del ensayo. Playwright se usa solo para verificar en Edge, no es dependencia propuesta del futuro conversor.
- results.json: métricas de conversión.
- browser-results.json: comprobaciones de imágenes, texto visible y anchura en Edge a 1000 px.
- text-comparison.json y source-text/: contraste con RichTextBox, ignorando espacios.
- output/RtfPipe/: HTML original de la biblioteca, captura PNG y PDF de Edge.
- output/RtfPipePng/: variante experimental con PNG incrustado, captura y PDF.
- output/Aspose/: HTML, capturas y PDF con marcas de evaluación.
- reference/: captura y PDF del HTML LibreOffice suministrado.

Se generaron capturas y PDF de los seis archivos para cada variante. Inspección visual manual realizada sobre Sobótka_1 (tres motores/referencia) y Polecenie (RtfPipePng); no se ha revisado visualmente cada página de todos los PDF. No hay comparación con impresiones originales de WordPad.

## Repetir

Desde Windows con SDK .NET 9 y Edge instalado:

```powershell
dotnet run --project .\evaluation\LibraryEvaluation -- "F:\RtfToHTML"
powershell -NoProfile -File .\evaluation\Convert-WmfImages.ps1 -Root "F:\RtfToHTML"
dotnet run --project .\evaluation\LibraryEvaluation -- "F:\RtfToHTML" --browser
```

Las conversiones y verificaciones se ejecutaron localmente. No se enviaron los documentos a servicios de conversión externos.

## Decisión propuesta

Prototipo gratuito: RtfPipe + rasterización de imágenes con Windows + HTML UTF-8 completo. Antes del ejecutable definitivo, corregir/probar alineaciones y saltos con los RTF reales y validar con el motor de impresión de la aplicación. Si se exige mayor fidelidad y se acepta licencia comercial, continuar la validación con Aspose sin restricciones de evaluación. Mantener la interfaz de dos parámetros en ambos casos.
