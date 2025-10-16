-- Script SQL para insertar datos directamente si los necesitas
-- Ejecuta esto en tu base PostgreSQL si es necesario

-- Insertar clientes si no existen
INSERT INTO "Clientes" ("Documento", "Nombre", "IngresosMensuales", "DeudaTotal", "EstadoMora", "FechaActualizacion")
SELECT * FROM (VALUES 
    ('12345678', 'Juan Pérez Martínez', 3500, 2500, 'Temprana', NOW()),
    ('87654321', 'María García López', 2200, 3800, 'Moderada', NOW()),
    ('11122233', 'Carlos Mendoza Silva', 4500, 1200, 'Al día', NOW()),
    ('44455566', 'Ana Torres Ruiz', 2800, 4200, 'Grave', NOW()),
    ('77788899', 'Luis Ramírez Castro', 1800, 5500, 'Crítica', NOW()),
    ('99988877', 'Sofia Herrera Vega', 3200, 2100, 'Temprana', NOW()),
    ('66655544', 'Miguel Flores Santos', 4000, 3300, 'Moderada', NOW()),
    ('33322211', 'Elena Morales Cruz', 2600, 1800, 'Al día', NOW())
) AS nuevos_clientes("Documento", "Nombre", "IngresosMensuales", "DeudaTotal", "EstadoMora", "FechaActualizacion")
WHERE NOT EXISTS (
    SELECT 1 FROM "Clientes" WHERE "Documento" = nuevos_clientes."Documento"
);

-- Insertar asignaciones para el asesor (reemplaza 'ASESOR_USER_ID' con el ID real del asesor)
-- Puedes obtener el ID del asesor con: SELECT "Id" FROM "AspNetUsers" WHERE "Email" = 'asesor@audicob.com';