Feature: Remove real-world Country/State/City data
  Not part of the regression suite -- lives outside com/mnemotoad/knowledge, which is the only
  package path TestRunner.java scans, so this never runs via `mvn test`. Run explicitly instead,
  e.g. `mvn test -Dtest=WorldDataLoader#delete` (see WorldDataLoader.java in this package).

  Background:
    * url baseUrl

  Scenario: Remove everything create-world-data.feature seeded, including flag media
    * json data = read('world-graph.json')
    * karate.call('classpath:com/mnemotoad/knowledge/testdata/cleanup-test-data.feature', { data: data })
