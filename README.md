# Azure Hoard

Organizador del escritorio de Windows con **zonas translúcidas (fences)** donde agrupas tus archivos, carpetas y accesos directos. Proyecto propio, inspirado en la idea de los organizadores de escritorio, con nombre, diseño y código originales.

> Versión 0.1.0, primera prueba. Está pensada para probarla primero en una máquina virtual.

## Qué hace esta versión
- Crea fences translúcidas con título; se mueven arrastrando el título y cambian de tamaño con la esquina inferior derecha.
- Arrastra archivos desde el Explorador o desde otra fence: **el archivo no se mueve de sitio**, la fence solo guarda una referencia.
- Doble clic en un icono lo abre. Clic derecho: Abrir, Mostrar en la carpeta, Quitar de esta zona.
- Doble clic en el título enrolla la fence. Clic derecho en el título: Renombrar, Enrollar, Nueva zona, Eliminar.
- La fence **Escritorio** muestra automáticamente todo lo que no está en otra fence y se actualiza sola.
- Mientras la app está abierta, los iconos reales del escritorio se esconden. Se vuelven a mostrar al salir.
- Icono en la bandeja (junto al reloj): nueva zona, mostrar u ocultar zonas, idioma (inglés, español, gallego, portugués), iniciar con Windows, abrir la carpeta de datos y salir.
- Guarda la posición y el contenido en `%AppData%\AzureHoard\layout.json`.

## Si los iconos del escritorio no vuelven
1. Ejecuta `AzureHoard.exe --restore`, o
2. Administrador de tareas, "Explorador de Windows", clic derecho, **Reiniciar**.

## Cómo probarla (VirtualBox, Windows 11)
1. En tu repositorio de GitHub, pestaña **Actions**, elige "Compilar Azure Hoard (.exe)" y pulsa **Run workflow**.
2. Cuando termine, descarga `AzureHoard-win-x64` desde **Artifacts** y descomprime el zip.
3. Copia `AzureHoard.exe` a la máquina virtual (carpeta compartida o arrastrando).
4. Windows puede avisar con "Windows protegió su PC" porque el .exe no está firmado: Más información, Ejecutar de todas formas.
5. Para que la transparencia vaya bien en la máquina virtual: activa la aceleración 3D en su configuración e instala las Guest Additions.

## Limitaciones conocidas de la 0.1
- Con **Win+D** (mostrar escritorio) las fences se recolocan solas, pero pueden parpadear.
- Si añades un archivo desde fuera del escritorio a la fence "Escritorio", no se muestra (esa fence solo enseña el contenido del escritorio).
- Sin pestañas, reglas automáticas, carpetas espejo ni guardar disposiciones todavía.

## Siguientes pasos
1. Reglas de ordenación automática por tipo de archivo.
2. Carpetas espejo (portals) y menú contextual completo de Windows.
3. Transparencia y desenfoque configurables, temas.
4. Guardar y restaurar disposiciones, varios monitores, pestañas.
5. Instalador y actualizaciones.

## Icono
`icon.svg` y `icon-512.png` (gema azul sobre escamas).
