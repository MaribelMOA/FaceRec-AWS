# FaceRec-AWS 👤📸

Proyecto en .NET Web API para reconocimiento facial usando la cámara del sistema, detección con OpenCV y comparación/registro en AWS Rekognition. Las visitas se almacenan localmente en un archivo `visits.json`.

---

## 🚀 ¿Qué hace este proyecto?

- Captura una imagen con la webcam.
- Detecta rostros con OpenCV.
- Consulta AWS Rekognition para reconocer o registrar rostros.
- Guarda y verifica visitas recientes (últimas 24h) en un archivo JSON local.

---

## 🛠 Requisitos previos

- ✅ Tener instalada una versión reciente de .NET (8)
- ✅ Cámara web disponible en la computadora con Windows
- ✅ Asegurarse de que el puerto 5116 esté libre antes de iniciar la API. Este proyecto utiliza por defecto la URL: http://localhost:5116
- ✅ Cuenta de AWS con permisos para Rekognition (crear un IAM User con AwsRekognitionFullAccess)
- ✅ Haber creado una colección facial en Rekognition con el nombre exacto:

  ```bash
  mi-coleccion-facial
  ```
  
  Puedes crearla desde la [Consola de Amazon Rekognition (Collections)](https://console.aws.amazon.com/rekognition/home#/collections) o por CLI con:
  
  ```bash
  aws rekognition create-collection --collection-id mi-coleccion-facial
  ```
- ✅  Crear un archivo .env con las credenciales de AWS:

  ```bash
  AWS_ACCESS_KEY_ID=TU_ACCESS_KEY
  AWS_SECRET_ACCESS_KEY=TU_SECRET_KEY
  AWS_REGION=us-west-2
  ```
---

## 📦 Instalación

1. Clona este repositorio:
  ```bash
  git clone https://github.com/MaribelMOA/FaceRec-AWS.git

  ```
2. Construye el proyecto:
  ```bash
dotnet build FaceRec-AWS.sln
  ```
3. Corre el servidor:

 ```bash
dotnet run
  ```
---

## 📌 Endpoints disponibles

1. ## POST http://localhost:5116/api/FaceRecognition/capture-and-check
📸 Descripción:
Captura una imagen desde la cámara conectada al servidor, detecta un rostro y lo envía a Amazon Rekognition para identificarlo o registrarlo si es nuevo.
Verifica si esa persona ha sido registrada en las últimas 24 horas en el archivo visits.json.

📥 Parámetros:
*Ninguno*

📤 Ejemplo de uso:

 ```bash
Copiar código
curl -X 'POST' \
  'http://localhost:5116/api/FaceRecognition/capture-and-check' \
  -H 'accept: */*' \
  -d ''
```
✅ Respuesta si es la primera vez que vistia esa persona en las pasadas 24 h:

 ```json
{
  "allowed": true,
  "face_id": "abcd1234-face-id",
  "external_image_id": "user123",
  "visits_count": 0
}
```
✅ Respuesta si la persona ha visitado en las pasadas 24 h:

 ```json
{
  "allowed": false,
  "face_id": "abcd1234-face-id",
  "external_image_id": "user123",
  "visits_count": 1
}
```
❌ Respuesta si no se detecta rostro o ocurre error:

 ```json
{
  "allowed": false,
  "message": "No face detected."
}
```
⚙️ Qué hace internamente:
- Usa OpenCV para capturar y recortar el rostro.
- Consulta Rekognition para buscar coincidencias.
- Si no hay coincidencia, lo registra como un nuevo rostro.
- Verifica si ya registró una visita en las últimas 24 horas.

2. ## POST http://localhost:5116/api/FaceRecognition/register-visit
📝 Descripción:
Registra manualmente una visita usando faceId y externalImageId.
Guarda la fecha y hora actual en el archivo visits.json.

📥 Body JSON requerido:

 ```json
{
  "faceId": "abcd1234-face-id",
  "externalImageId": "user123"
}
```
📤 Ejemplo de uso con cURL:

 ```bash
curl -X 'POST' \
  'http://localhost:5116/api/FaceRecognition/register-visit' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
  "faceId": "abcd1234-face-id",
  "externalImageId": "user123"
}'
 ```
✅ Respuesta:

 ```json
{
  "success": true
}
 ```
⚙️ Qué hace internamente:
- Abre o crea visits.json.
- Agrega un nuevo registro con la fecha y hora actuales.


3. ## POST http://localhost:5116/api/FaceRecognition/register-image
📸 Descripción:
Sube la imagen temporal capturada por la ruta capture-and-check a S3 (o tu storage configurado) y elimina el archivo local.

📥 Parámetros:
 - *tempFileName* (string): Nombre del archivo temporal (ej. "face_20250709_123456_1a2b3c4d.jpg").
  - *realFileName* (string): (Opcional) Prefijo personalizado para la imagen final; si está vacío se genera uno automático con timestamp


📤 Ejemplo de uso con cURL:

 ```bash
curl -X POST \
  'http://localhost:5116/api/FaceRecognition/register-image?tempFileName=face_20250709_123456_1a2b3c4d.jpg&realFileName=user123' \
  -H 'accept: application/json'

 ```
✅ Respuesta:

 ```json
  {
    "success": true,
    "imageUrl": "https://tu-bucket.s3.amazonaws.com/visitas/user123_20250709_123456.jpg"
  }

 ```
⚙️ Qué hace internamente:
- Busca el archivo temp-images/{tempFileName}.
- Genera un nombre final visitas/{realFileName or GUID}_{timestamp}.jpg.
- Sube el archivo a S3 (o storage configurado).
- Elimina el archivo temporal local.

4. ## POST http://localhost:5116/api/FaceRecognition/delete-tempImage/{tempFileName}  
 Descripción:
Elimina el archivo temp-images/{tempFileName} de disco.

📥 Parámetros:
 - *tempFileName* (string): Nombre del archivo temporal (ej. "face_20250709_123456_1a2b3c4d.jpg").

📤 Ejemplo de uso con cURL:

 ```bash
  curl -X DELETE \
    'http://localhost:5116/api/FaceRecognition/delete-tempImage/face_20250709_123456_1a2b3c4d.jpg' \
    -H 'accept: application/json'

 ```
✅ Respuesta:

 ```json
  { "success": true, "message": "Temp image deleted successfully" }
 ```

❌ En caso de fallos y mensajes

 ```json
  { "success": true, "message": "Temp image deleted successfully" }
 ```
5. ## GET http://localhost:5116/api/FaceRecognition/get-image?fileName={fileName} 
 Descripción:
  ¿Qué hace internamente?
    1. Si fileName contiene .jpg y _, lo usa directamente como clave S3 visitas/{fileName}.
    2. Si no, busca el objeto más reciente con prefijo visitas/{fileName}_ mediante ListObjectsV2.
    3. Genera una URL firmada válida 60 min.

📥 Parámetros:
 - *fileName* (string, requerido): Nombre completo o prefijo. (ej. "orderTransaction1_20250709_130956.jpg" O "orderTransaction1").

📤 Ejemplo de uso con cURL:

 ```bash
curl -X GET \
  'http://localhost:5116/api/FaceRecognition/get-image?fileName=orderTransaction1' \
  -H 'accept: application/json'


 ```
✅ Respuesta exitosa:

 ```json
  { "success": true, "url": "https://tu-bucket.s3.amazonaws.com/visitas/user123_20250709_123456.jpg" }

 ```

❌ En caso de fallos y mensajes
- Parametro invalido:
 ```json
  { "success": true, "message": "Temp image deleted successfully" }
 ```
 - No encontrado:
 ```json
  { "success": false, "message": "No image found with that name." }
 ```
6. ## DELETE http://localhost:5116/api/FaceRecognition/delete-image/{fileName} 
 Descripción:
  ¿Qué hace internamente?
    1. Igual que get-image, resuelve la clave S3.
    2. Llama a DeleteObjectAsync.
    3. Devuelve éxito o fallo.

📥 Parámetros:
 - *fileName* (string, requerido): Nombre completo o prefijo. (ej. "orderTransaction1_20250709_130956.jpg" O "orderTransaction1").

📤 Ejemplo de uso con cURL:

 ```bash
curl -X DELETE \
  'http://localhost:5116/api/FaceRecognition/delete-image/user123' \
  -H 'accept: application/json'

 ```
✅ Respuesta exitosa:

 ```json
 { "success": true, "message": "Imagen 'visitas/user123_20250709_123456.jpg' eliminada exitosamente." }
 ```

❌ En caso de fallos y mensajes
- Parametro invalido:
 ```json
  { "success": false, "message": "You must provide a file name" }
 ```
 - No encontrado:
 ```json
  { "success": false, "message": "No se pudo borrar el archivo o no existe." }
 ```

7. ## GET http://localhost:5116/api/FaceRecognition/images-by-date?date={yyyyMMdd}
 Descripción:
  ¿Qué hace internamente?
    1. Lista con ListObjectsV2 todos los objetos bajo visitas/.
    2. Filtra por Key.Contains(date).
    3. Devuelve URLs firmadas (o directas) de cada coincidencia.

📥 Parámetros:
 - *date* (string, requerido):  Formato yyyyMMdd (p.e. "20250709").

📤 Ejemplo de uso con cURL:

 ```bash
curl -X GET \
  'http://localhost:5116/api/FaceRecognition/images-by-date?date=20250709' \
  -H 'accept: application/json'

 ```
✅ Respuesta exitosa:

 ```json
  {
    "success": true,
    "count": 3,
    "images": [
      "https://tu-bucket.s3.amazonaws.com/visitas/userA_20250709_101010.jpg",
      "https://tu-bucket.s3.amazonaws.com/visitas/userB_20250709_111111.jpg",
      "https://tu-bucket.s3.amazonaws.com/visitas/userC_20250709_121212.jpg"
    ]
  }

 ```

❌ En caso de fallos y mensajes
- Parametro invalido:
 ```json
  { "success": false, "message": "Debe proporcionar una fecha (formato: yyyyMMdd)." }
 ```
 - Sin resultados:
 ```json
  { "success": false, "message": "No se encontraron imágenes para esa fecha." }

 ```

