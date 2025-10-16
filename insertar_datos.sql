-- INSERTAR CLIENTES DIRECTAMENTE
INSERT INTO "Clientes" ("Documento", "Nombre", "IngresosMensuales", "DeudaTotal", "EstadoMora", "FechaActualizacion")
VALUES 
('12345678', 'Juan Pérez López', 3000, 1500, 'Temprana', NOW()),
('87654321', 'María García Silva', 2500, 2800, 'Moderada', NOW()),
('11223344', 'Carlos Mendoza Cruz', 4000, 5200, 'Grave', NOW())
ON CONFLICT ("Documento") DO NOTHING;

-- INSERTAR ASIGNACIONES DIRECTAMENTE
INSERT INTO "AsignacionesAsesores" ("ClienteId", "AsesorUserId", "AsesorNombre", "FechaAsignacion")
SELECT 
    c."Id",
    u."Id",
    'Asesor de Cobranza',
    NOW()
FROM "Clientes" c
CROSS JOIN "AspNetUsers" u
WHERE u."Email" = 'asesor@audicob.com'
AND c."Documento" IN ('12345678', '87654321', '11223344')
ON CONFLICT DO NOTHING;

-- VERIFICAR QUE SE INSERTARON
SELECT 'Clientes insertados:' as info, COUNT(*) as cantidad FROM "Clientes";
SELECT 'Asignaciones insertadas:' as info, COUNT(*) as cantidad FROM "AsignacionesAsesores";