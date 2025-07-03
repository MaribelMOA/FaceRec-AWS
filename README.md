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

3. ## POST http://localhost:5116/api/FaceRecognition/check-and-register
📸 Descripción:
Captura una imagen desde la cámara conectada, verifica si el rostro ya está registrado en Amazon Rekognition y lo registra si es nuevo. Sin importar si ya visitó en las últimas 24 horas, guarda el registro de la visita en el archivo visits.json.

📥 Parámetros:
*Ninguno*

📤 Ejemplo de uso:

 ```bash
curl -X 'POST' \
  'http://localhost:5116/api/FaceRecognition/check-and-register' \
  -H 'accept: */*' \
  -d ''

```
✅ Respuesta si es la primera vez que vistia esa persona en las pasadas 24 h:

 ```json
{
  "allowed": true,
  "face_id": "abcd1234-face-id",
  "external_image_id": "user123",
 "visits_count": 1,
  "registered": true
}
```
✅ Respuesta si la persona ha visitado en las pasadas 24 h:

 ```json
{
  "allowed": false,
  "face_id": "abcd1234-face-id",
  "external_image_id": "user123",
  "visits_count": 2, //Numero mayor a 1
  "registered": true
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
- Captura y guarda una imagen desde la cámara.
- Utiliza AWS Rekognition para verificar si el rostro ya existe.
- Si no existe, lo registra.
- Agrega un nuevo registro de visita en visits.json con la fecha y hora actual.
- Indica si el rostro había visitado en las últimas 24 horas.

4. ## GET http://localhost:5116/api/FaceRecognition/visits-on-date
📝 Descripción:
Elimina todos los registros de visita del archivo visits.json.Devuelve el número de visitantes únicos que han sido registrados en una fecha específica (por defecto, hoy). Se cuentan como únicos por FaceId y ExternalImageId.

📥 Parámetros:
- *date* (opcional): Fecha a consultar en formato YYYY-MM-DD. Si no se proporciona, se usa la fecha actual del servidor.
📤 Ejemplo de uso:

 ```bash
curl -X 'GET' \
  'http://localhost:5116/api/FaceRecognition/visits-on-date?date=2025-07-02' \
  -H 'accept: */*'
 ```
✅ Respuesta exitosa:

 ```json
  {
    "success": true,
    "total_visits": 5,
    "unique_visitors": 2,
    "details": [
      {
        "face_id": "abc123",
        "external_image_id": "user1",
        "visit_count": 3
      },
      {
        "face_id": "xyz789",
        "external_image_id": "user2",
        "visit_count": 2
      }
    ]
  }

 ```
❌ Respuesta si no hay registros para la fecha:

 ```json
  {
    "success": true,
    "count": 0,
    "message": "No visits found for the specified date.",
    "date": "2025-07-02"
  }

 ```
❌ Errores posibles:

 ```json
  {
    "success": false,
    "message": "visists.json file not found."
  }
```

⚙️ Qué hace internamente:
- Verifica si existe el archivo visits.json.
- Filtra todas las visitas del día indicado.
- Agrupa por FaceId y ExternalImageId para obtener visitantes únicos y sus frecuencias.
- Devuelve:
  - *total_visits*: número total de registros encontrados en esa fecha.
  - *unique_visitors*: cantidad de personas distintas.
  - *details*: lista de cada persona con su número de visitas.
  
5.  ## GET http://localhost:5116/api/FaceRecognition/get-all-visits
📝 Descripción:
Devuelve todas las visitas registradas en el archivo visits.json, sin importar la fecha. Útil para obtener el historial completo.

📥 Parámetros:
 *Ninguno*.
📤 Ejemplo de uso:

 ```bash
curl -X 'GET' \
  'http://localhost:5116/api/FaceRecognition/get-all-visits' \
  -H 'accept: */*'
 ```
✅ Respuesta exitosa:

 ```json
  {
    "success": true,
    "count": 4,
    "visits": [
      {
        "faceId": "abc123",
        "externalImageId": "user1",
        "timestamp": "2025-07-01T10:12:00"
      },
      {
        "faceId": "xyz789",
        "externalImageId": "user2",
        "timestamp": "2025-07-01T12:45:00"
      },
      ...
    ]
  }

 ```
✅ Respuesta  si no hay visitas:

 ```json
  {
  "success": true,
  "count": 0,
  "message": "No visits recorded"
}
 ```

❌ Errores posibles:

 ```json
  {
    "success": false,
    "message": "visits.json file not found."
  }

```

⚙️ Qué hace internamente:
- Verifica si existe el archivo visits.json.
- Si existe, lo deserializa y devuelve la lista completa de visitas.
- Si no hay visitas o el archivo está vacío, devuelve un mensaje informativo con count = 0


6. ## DELETE http://localhost:5116/api/FaceRecognition/delete-last-visit
📝 Descripción:
Elimina el último registro almacenado en el archivo visits.json. Útil para pruebas o corrección de errores.

📥 Parámetros:
*Ninguno*
📤 Ejemplo de uso:

 ```bash
curl -X 'DELETE' \
  'http://localhost:5116/api/FaceRecognition/delete-last-visit' \
  -H 'accept: */*'

 ```
✅ Respuesta:

 ```json
{
  "success": true
}
 ```
❌ Errores posibles:
 - Si el archivo no existe:
 ```json
  {
    "success": false,
    "message": "visits.json file not found."
  }
```

 - Si no hay visitas para eliminar:

 ```json
  {
    "success": false,
    "message": "No visits to delete."
  }

```

⚙️ Qué hace internamente:
- Carga el archivo visits.json.
- Si existen visitas registradas, elimina la última.
- Guarda el nuevo contenido en el mismo archivo



7. ## DELETE http://localhost:5116/api/FaceRecognition/delete-visits-on-date
📝 Descripción:
Elimina todos los registros de visita correspondientes a una fecha específica (o a la fecha actual si no se indica).

📥 Parámetros:
- *date* (opcional): Fecha a consultar en formato YYYY-MM-DD. Si no se proporciona, se usa la fecha actual del servidor.
📤 Ejemplo de uso:

 ```bash
curl -X 'DELETE' \
  'http://localhost:5116/api/FaceRecognition/delete-visits-on-date?date=2025-07-02' \
  -H 'accept: */*'
 ```
✅ Respuesta exitosa:

 ```json
 {
    "success": true,
    "deleted": 5,
    "date": "2025-07-02"
  }

 ```
❌ Errores posibles:
 - Si no hay visitas:
 ```json
  {
    "success": true,
    "deleted": 0,
    "message": "No visits to delete."
  }
```
 - Si no existe el archivo::

 ```json
  {
    "success": false,
    "message": "visits.json file not found."
  }
```

⚙️ Qué hace internamente:
- Carga todas las visitas desde visits.json.
- Elimina las que coinciden con la fecha especificada.
- Sobrescribe el archivo con las visitas restantes.

8. ## DELETE http://localhost:5116/api/FaceRecognition/delete-all-visits
📝 Descripción:
Elimina todos los registros de visita del archivo visits.json.

📥 Parámetros:
*Ninguno* 
📤 Ejemplo de uso:

 ```bash
curl -X 'DELETE' \
  curl -X 'DELETE' \
  'http://localhost:5116/api/FaceRecognition/delete-all-visits' \
  -H 'accept: */*'

 ```
✅ Respuesta exitosa:

 ```json
  {
    "success": true,
    "message": "Todos los registros de visitas han sido eliminados."
  }


 ```
❌ Errores posibles:

 ```json
  {
    "success": false,
    "message": "visits.json file not found."
  }

```

⚙️ Qué hace internamente:
- Verifica si existe el archivo visits.json.
- Si existe, lo sobrescribe con una lista vacía ([]).
