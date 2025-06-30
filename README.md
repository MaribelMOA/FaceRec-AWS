# FaceRec-AWS 👤📸🤖

Proyecto en .NET Web API para reconocimiento facial usando la cámara del sistema, detección con OpenCV y comparación/registro en AWS Rekognition. Las visitas se almacenan localmente en un archivo `visits.json`.

---

## 🚀 ¿Qué hace este proyecto?

- Captura una imagen con la webcam.
- Detecta rostros con OpenCV.
- Consulta AWS Rekognition para reconocer o registrar rostros.
- Guarda y verifica visitas recientes (últimas 24h) en un archivo JSON local.

---

## 🛠 Requisitos previos

- ✅ Tener instalada una versión reciente de .NET (6 o superior)
- ✅ Cámara web disponible en la computadora con Windows
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
cd FaceRec-AWS
  ```
2. Instala dependencias:
  ```bash
dotnet add package OpenCvSharp4
dotnet add package OpenCvSharp4.runtime.win
dotnet add package AWSSDK.Rekognition
dotnet add package Swashbuckle.AspNetCore
dotnet add package DotNetEnv
  ```
3. Construye el proyecto:
 ```bash
dotnet build
  ```

4. Corre el servidor:

 ```bash
dotnet run
  ```
---

## 📌 Endpoints disponibles

1. ## POST /api/FaceRecognition/capture-and-check
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

2. ## POST /api/FaceRecognition/register-visit
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
