package com.mnemotoad.worlddata;

import com.intuit.karate.Runner;
import com.intuit.karate.Results;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.assertEquals;

// Deliberately NOT named *Test/*Tests/*TestCase/Test* -- Surefire's default `mvn test` discovery
// glob won't pick this class up, so it only ever runs when explicitly requested with -Dtest,
// same as this package's two .feature files are excluded from TestRunner's classpath scan (see
// create-world-data.feature/delete-world-data.feature for why). Two independent methods so
// create and delete can be run separately:
//   mvn test -Dtest=WorldDataLoader#create
//   mvn test -Dtest=WorldDataLoader#delete
class WorldDataLoader {

    @Test
    void create() {
        Results results = Runner.path("classpath:com/mnemotoad/worlddata/create-world-data.feature").parallel(1);
        assertEquals(0, results.getFailCount(), results.getErrorMessages());
    }

    @Test
    void delete() {
        Results results = Runner.path("classpath:com/mnemotoad/worlddata/delete-world-data.feature").parallel(1);
        assertEquals(0, results.getFailCount(), results.getErrorMessages());
    }
}
