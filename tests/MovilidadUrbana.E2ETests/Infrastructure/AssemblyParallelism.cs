// Las clases de prueba corren en paralelo entre sí; dentro de cada una, en secuencia.
//
// No se sube a `ParallelScope.Children`: la integración de Playwright con NUnit lleva un registro
// de servicios por worker, y al paralelizar dentro de una misma clase se rompe con
// «The given key 'Browser' was not present in the dictionary». Es la diferencia con el runner de
// JavaScript, donde `fullyParallel: true` reparte prueba por prueba.
//
// Acá va el *alcance* del paralelismo, que es una decisión del código. La *cantidad* de workers
// vive únicamente en `pruebas.runsettings` (`NumberOfTestWorkers`), para que haya una sola fuente
// de verdad: declararla también con `[assembly: LevelOfParallelism]` deja dos números que pueden
// divergir sin que nada avise.
[assembly: Parallelizable(ParallelScope.Fixtures)]
