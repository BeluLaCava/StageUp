const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const source = fs.readFileSync(path.join(__dirname, '../StageUp.UI/Scripts/detalle-espacio.js'), 'utf8');
function extract(name) {
    const start = source.indexOf('function ' + name + '(');
    assert(start >= 0);
    let i = source.indexOf('{', start), level = 0;
    do { if (source[i] === '{') level++; if (source[i] === '}') level--; i++; } while (level > 0 && i < source.length);
    return source.slice(start, i);
}
const ranges = new Function('rows', 'occupiedRows', 'dateValue',
    ['restarSegmentos', 'franjasParaFecha', 'rangesForDate'].map(extract).join('\n') + '\nreturn rangesForDate(dateValue);');
const open = { DiaSemana: 1, Fecha: null, MinutoDesde: 540, MinutoHasta: 720, Bloqueado: false };
const monthly = { DiaSemana: 1, SemanaDelMes: 1, Fecha: null, MinutoDesde: 600, MinutoHasta: 660, Bloqueado: true };
const hours = r => r.map(x => [x.MinutoDesde, x.MinutoHasta]);
const split = [[540,600],[660,720]], full = [[540,720]];
assert.deepEqual(hours(ranges([open,monthly], [], '2028-01-03')), split);
assert.deepEqual(hours(ranges([open,monthly], [], '2031-01-06')), split);
assert.deepEqual(hours(ranges([open,monthly], [], '2028-01-10')), full);
const last = {...monthly, SemanaDelMes:5};
assert.deepEqual(hours(ranges([open,last], [], '2028-02-28')), split);
assert.deepEqual(hours(ranges([open,last], [], '2028-01-31')), split);
assert.deepEqual(hours(ranges([open,last], [], '2028-01-24')), full);
assert.deepEqual(hours(ranges([open,{...monthly,SemanaDelMes:4}], [], '2028-01-24')), split);
assert.deepEqual(hours(ranges([open,{...monthly,SemanaDelMes:4}], [], '2028-01-31')), full);
assert.deepEqual(hours(ranges([open,{...monthly,SemanaDelMes:null}], [], '2028-01-10')), split);
assert.deepEqual(hours(ranges([open,{...monthly,SemanaDelMes:null,Fecha:'2028-01-10'}], [], '2028-01-03')), full);
assert.deepEqual(hours(ranges([open,monthly], [{Fecha:'2028-01-03',MinutoDesde:660,MinutoHasta:720}], '2028-01-03')), [[540,600]]);
assert.deepEqual(hours(ranges([open,monthly,{...open,Fecha:'2028-01-03',DiaSemana:null,MinutoDesde:600,MinutoHasta:690}], [], '2028-01-03')), [[660,690]]);
console.log('12 comprobaciones del planificador mensual correctas');
