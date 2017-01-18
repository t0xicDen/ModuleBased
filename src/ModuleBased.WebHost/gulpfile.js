/// <binding AfterBuild='copy-modules' />
"use strict";

var gulp = require("gulp"),
    clean = require('gulp-clean');

var paths = {
    devModules: "../Modules/",
    hostModules: "./Modules/"
};

var modules = [
    'ModuleBased.Module.Example'
];

gulp.task('clean-module', function () {
    return gulp.src([paths.hostModules + '*'], { read: false })
    .pipe(clean());
});

gulp.task('copy-modules', ['clean-module'], function () {
    modules.forEach(function (module) {
        gulp.src([paths.devModules + module + '/Views/**/*.*', paths.devModules + module + '/bin/**/*.Module.*.*', paths.devModules + module + '/wwwroot/**/*.*'], { base: module })
            .pipe(gulp.dest(paths.hostModules + module));
    });
});
