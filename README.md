# RtfToHTML

Prototipo Windows de conversión RTF a HTML con RtfPipe integrado. No requiere Word ni LibreOffice. Mantiene tablas y estilos, convierte imágenes WMF/EMF a PNG incrustado y respeta la justificación centrada/derecha de las tablas del RTF.

```powershell
.\artifacts\publish\RtfToHTML.exe ".\samples\Sobótka_1.rtf" ".\salida.html"
```

Dos parámetros obligatorios: archivo RTF de entrada y ruta HTML de salida. El directorio de salida debe existir. Un HTML existente se reemplaza solo después de completar la conversión. Códigos: 0 correcto, 1 error de conversión/archivo, 2 número de parámetros incorrecto. Los errores se escriben en stderr.

## Compilar y publicar

Windows y SDK .NET 9 para desarrollar:

```powershell
dotnet build src/RtfToHtml
dotnet publish src/RtfToHtml -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/publish
```

La publicación incluye el runtime: no exige instalar .NET en el equipo destino. Distribuir también RtfPipe.LICENSE.txt. El ejecutable usa GDI+ de Windows para rasterizar las imágenes vectoriales.

## Verificación

Con Microsoft Edge instalado:

```powershell
dotnet run --project tests/Regression -- .
```

El ensayo comprueba geometría de tablas izquierda/centro/derecha, sangría izquierda, texto de celdas, cabeceras y tablas anidadas. Convierte los seis archivos de samples y compara texto visible (ignorando espacios), filas, tablas e imágenes con la evaluación anterior. Comprueba la posición del bloque del cliente en Sobótka_1 en medios pantalla e impresión. Guarda resultados, HTML, capturas y PDF en artifacts/alignment.

Playwright y Edge solo se utilizan para las pruebas, no para convertir. Aspose permanece únicamente en el proyecto histórico evaluation/LibraryEvaluation; el nuevo ejecutable no lo referencia.

## Alcance de la corrección

RtfPipe reconocía trqc/trqr, pero no trasladaba esos tokens al CSS de la tabla. La copia local en vendor/RtfPipe corrige ese recorrido. La alineación procede de la primera fila del RTF, sin reglas por nombre de archivo o texto del cliente. Ver vendor/RtfPipe/LOCAL_CHANGES.md.

La corrección no garantiza una paginación idéntica a WordPad: siguen pendientes de validación los márgenes de página, fuentes, espaciado vertical y otros tipos de posicionamiento. La rasterización actual conserva el lienzo de la imagen; resolución y recorte requieren más validación con documentos distintos.

samples contiene copias de los seis RTF proporcionados. evaluation conserva los ensayos previos y sus rutas históricas; artifacts contiene las salidas nuevas y queda excluido de Git. Los documentos originales no se modifican.
