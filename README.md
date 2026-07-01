# IASD Magnolia

Sistema web desarrollado en **Blazor Server (.NET 8)** para la administración y publicación del sitio web de la **Iglesia Adventista del Séptimo Día Magnolia**.

El proyecto permite administrar el contenido del sitio desde un panel administrativo y mostrar la información al público mediante una interfaz moderna y organizada.

---

# Tecnologías

- .NET 8
- Blazor Server
- PostgreSQL
- Dapper
- Npgsql
- Bootstrap
- HTML / CSS / JavaScript

---

# Arquitectura

El proyecto está dividido en tres capas principales.

```
Components
│
├── Presentación (UI)
│
Services
│
├── Lógica de negocio
│
Models
│
└── Objetos de datos
```

El flujo siempre debe seguir este orden:

```
Component
      ↓
Service
      ↓
PostgreSQL
```

Los **Components nunca deben acceder directamente a la base de datos**.

Toda la lógica debe implementarse dentro de **Services**.

---

# Estructura del Proyecto

## Components

Contiene toda la interfaz del usuario.

Aquí se encuentran:

- Páginas públicas
- Panel Administrativo
- Componentes reutilizables
- Formularios
- Navegación
- Layouts

Su responsabilidad es únicamente mostrar información y llamar a los Services.

---

## Services

Aquí vive toda la lógica del sistema.

Los Services son responsables de:

- Consultar PostgreSQL
- Insertar registros
- Actualizar información
- Eliminar registros
- Validar reglas de negocio
- Manejar autenticación
- Registrar errores

Los Components solamente consumen estos Services.

---

## Models

Representan las entidades utilizadas por la aplicación.

Ejemplos:

- Usuario
- Evento
- Departamento
- Liderazgo
- Recursos

Los Models contienen únicamente la información necesaria para representar los datos.

---

## Program.cs

Configura toda la aplicación.

Aquí se registran:

- Dependency Injection
- Services
- Middleware
- Configuración de Blazor
- Cadena de conexión
- Seguridad

---

## appsettings.json

Contiene la configuración general.

Ejemplo:

- Connection Strings
- Configuración del sistema
- Variables generales

No deben almacenarse credenciales sensibles en producción.

---

# Flujo General

Cuando un usuario abre una página ocurre el siguiente proceso.

```
Usuario

↓

Component

↓

Service

↓

PostgreSQL

↓

Service

↓

Component

↓

Usuario
```

---

# Autenticación

El sistema utiliza un servicio de autenticación personalizado.

Su responsabilidad es:

- Validar credenciales
- Mantener la sesión
- Controlar permisos
- Restringir acceso al panel administrativo

---

# Organización

Cada módulo intenta mantenerse independiente.

Ejemplos:

- Eventos
- Departamentos
- Liderazgo
- Recursos
- Usuarios

Cada módulo contiene sus propios Components, Services y Models relacionados.

---

# Buenas Prácticas

Al agregar nuevas funcionalidades seguir siempre estas reglas.

✅ Nunca acceder directamente a PostgreSQL desde un Component.

✅ Toda la lógica debe ir en Services.

✅ Mantener los Models simples.

✅ Utilizar Dependency Injection.

✅ Registrar errores utilizando ILogger.

✅ Mantener una estructura consistente con el resto del proyecto.

---

# Objetivo

El objetivo del proyecto es mantener un sistema fácil de mantener, escalable y organizado, permitiendo agregar nuevas funcionalidades sin afectar las existentes.

La arquitectura busca separar claramente la interfaz de usuario, la lógica del negocio y el acceso a los datos para facilitar el mantenimiento futuro.

---

# Mantenimiento

Si vas a modificar el proyecto:

1. Comprende primero el flujo Component → Service → Database.
2. Reutiliza Services existentes antes de crear nuevos.
3. Evita duplicar lógica.
4. Mantén la misma estructura de carpetas.
5. Documenta cualquier cambio importante.

---

# Estado del Proyecto

Arquitectura estable.

Separación de responsabilidades correcta.

Proyecto preparado para continuar creciendo mediante nuevos módulos y funcionalidades.

/admin/login