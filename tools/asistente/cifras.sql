-- Oráculo independiente de las cifras de la encuesta (K3 R8.1): la misma pregunta que responde
-- ResultadosDeEncuestaService, escrita en SQL directo sobre la tabla, sin pasar por el código de
-- la aplicación. Si las dos vías no coinciden, una de las dos está mal.
-- Parámetro: @sesion. Salida: variable, clave, valor.

SELECT 'encuestas' AS variable, 'total' AS clave, COUNT(*) AS valor
FROM Encuestas WHERE SessionId = @sesion
UNION ALL
SELECT 'medios', m.clave, (SELECT COUNT(*) FROM Encuestas e WHERE e.SessionId = @sesion AND (',' || e.Medios || ',') LIKE '%,' || m.clave || ',%')
FROM (SELECT 'colectivo' AS clave UNION ALL SELECT 'auto' UNION ALL SELECT 'bicicleta' UNION ALL SELECT 'moto' UNION ALL SELECT 'caminata' UNION ALL SELECT 'tren') m
UNION ALL
SELECT 'motivos', Motivo, COUNT(*) FROM Encuestas WHERE SessionId = @sesion GROUP BY Motivo
UNION ALL
SELECT 'frecuencias', Frecuencia, COUNT(*) FROM Encuestas WHERE SessionId = @sesion GROUP BY Frecuencia
UNION ALL
SELECT 'minutos_promedio', 'colectivo', ROUND(AVG(Minutos), 1) FROM Encuestas WHERE SessionId = @sesion AND (',' || Medios || ',') LIKE '%,colectivo,%'
UNION ALL
SELECT 'distancia_promedio', 'colectivo', ROUND(AVG(Distancia), 1) FROM Encuestas WHERE SessionId = @sesion AND (',' || Medios || ',') LIKE '%,colectivo,%';
