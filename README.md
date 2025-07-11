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

---

## 🔄 Cambiar el proveedor de almacenamiento (AWS S3 / Google Cloud /Otro)

Este proyecto permite usar **Amazon S3** o **Google Cloud Storage** como sistema de almacenamiento para las imágenes capturadas.  Puedes alternar entre ambos de forma sencilla, o incluso extenderlo para usar cualquier otro sistema como Azure Blob Storage, Firebase Storage, etc.

### 🔧 Paso 1: Define las variables en tu archivo `.env`

#### ✅ Para usar **AWS S3**, incluye:

```bash
  AWS_ACCESS_KEY=tu_access_key
  AWS_SECRET_KEY=tu_secret_key
  AWS_REGION=us-west-2
```
#### ✅ Para usar **Google Cloud Storage**, incluye:

```bash
  GC_PROJECT_ID=tu_project_id
  GC_CLIENT_EMAIL=servicio@tu-proyecto.iam.gserviceaccount.com
  GC_CLIENT_ID=xxxxxxxxxxxxxxx
  GC_PRIVATE_KEY_ID=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
  GC_PRIVATE_KEY="-----BEGIN PRIVATE KEY-----\\nABC123...\\n-----END PRIVATE KEY-----\\n"
  GC_CLIENT_CERT_URL=https://www.googleapis.com/robot/v1/metadata/x509/...

```

🔐 Importante: La clave privada (GC_PRIVATE_KEY) debe colocarse como una sola línea entre comillas dobles y con los saltos de línea escapados (\\n).

### 🧩 Paso 2: Cambia la implementación de  `IStorageService` en `Program.cs`
Abre el archivo *Program.cs* y registra el proveedor deseado:

#### ➕ Para usar **AWS S3**:

```csharp
  builder.Services.AddSingleton<IStorageService, S3StorageService>();
```
#### ➕ Para usar **Google Cloud Storage**:

```csharp
  builder.Services.AddSingleton<IStorageService, GcpStorageService>();
```
---
### 🧱 ¿Quieres usar otro proveedor de almacenamiento?
El proyecto está diseñado para ser extensible. Solo necesitas:

1. Crear una nueva clase (por ejemplo, AzureBlobStorageService.cs).
2. Hacer que implemente la interfaz IStorageService.
3. Implementar los métodos:
      - UploadFileAsync
      - GetFileUrlAsync
      - FindFileByPrefixAsync
      - DeleteFileAsync
      - GetFilesByKeywordAsync
4. Registrar tu nueva clase en Program.c


```csharp
  builder.Services.AddSingleton<IStorageService, AzureBlobStorageService>();

```
Esto permite adaptar fácilmente el sistema a cualquier backend de almacenamiento que necesites sin modificar el resto del código.

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
curl -X 'POST' \
  'http://localhost:5116/api/FaceRecognition/capture-and-check' \
  -H 'accept: */*' \
  -d ''
```
✅ Respuesta si es la primera vez que vistia esa persona en las pasadas 24 h:

 ```json
{
  "face_id": "abcd1234-face-id",
  "external_image_id": "user123",
  "image_file_path":"face_20250709_123629_3c8de419.jpg"
}
```
✅ Respuesta si la persona ha visitado en las pasadas 24 h:

 ```json
{
  "face_id": "abcd1234-face-id",
  "external_image_id": "user123",
  "image_file_path":"face_20250709_123629_3c8de419.jpg"
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

 ```
✅ Respuesta exitosa:

 ```json
<