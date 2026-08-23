// Las clases de prueba corren en paralelo entre sí; dentro de cada una, en secuencia.
//
// No se sube a `ParallelScope.Children`: la integración de Playwright con NUnit lleva un registro
// de servicios por worker, y al paralelizar dentro de una misma clase se rompe con
// «The given key 'Browser' was not present in the dictionary». Es la diferencia con el runner de
// JavaScript, donde `fullyParallel: true` reparte prueba por prueba.
[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelism(3)]
