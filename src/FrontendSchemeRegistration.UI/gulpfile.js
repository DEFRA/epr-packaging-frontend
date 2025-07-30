const gulp = require('gulp');
const rename = require('gulp-rename');
const uglify = require('gulp-uglify');
const sass = require('gulp-sass')(require('sass'));
const path = require('path');

const deprecationSuppressions = ["import", "mixed-decls", "global-builtin"];
const paths = {
    "govuk": "node_modules/govuk-frontend/dist/govuk/"
};

let loadPaths = [
    path.join(__dirname, "node_modules"),
    path.join(__dirname, paths.govuk)
];

const sassOptions = {
    loadPaths: loadPaths,
    outputStyle: 'compressed',
    quietDeps: true,
    silenceDeprecations: deprecationSuppressions
};

gulp.task('compile-scss', () => {
  return gulp.src('assets/scss/application.scss')
    .pipe(sass(sassOptions, ''))
    .pipe(gulp.dest('wwwroot/css', { overwrite: true }));
});

gulp.task('copy-fonts', () => {
  return gulp.src(path.join(paths.govuk, 'assets/fonts/*'))
    .pipe(gulp.dest('wwwroot/fonts', { overwrite: true }));
});

gulp.task('copy-govuk-images', () => {
    return gulp.src(path.join(paths.govuk, 'assets/images/*'))
    .pipe(gulp.dest('wwwroot/images', { overwrite: true }));
});

gulp.task('copy-govuk-javascript', () => {
    return gulp.src(path.join(paths.govuk, 'all.bundle.js'))
    .pipe(uglify())
    .pipe(rename('govuk.js'))
    .pipe(gulp.dest('wwwroot/js', { overwrite: true }));
});

gulp.task('copy-custom-javascript', () => {
  return gulp.src('assets/js/*.js')
    .pipe(uglify())
    .pipe(gulp.dest('wwwroot/js', { overwrite: true }));
});

gulp.task('copy-custom-images', () => {
  return gulp.src('assets/images/*')
      .pipe(gulp.dest('wwwroot/images', { overwrite: true }));
});

gulp.task('copy-rebrand', () => {
    return gulp.src(path.join(paths.govuk, '/assets/rebrand/**/*'), { base: path.join(paths.govuk, '/assets/rebrand') })
        .pipe(gulp.dest('wwwroot/rebrand'));
});

gulp.task('copy-javascript', gulp.series('copy-govuk-javascript', 'copy-custom-javascript'));

gulp.task('copy-images', gulp.series('copy-govuk-images', 'copy-custom-images'));

gulp.task('build-frontend', gulp.series('compile-scss', 'copy-fonts', 'copy-images', 'copy-javascript', 'copy-rebrand'));