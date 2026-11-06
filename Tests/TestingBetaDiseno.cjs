// Isolated JavaScript regression checks. No browser, HTTP, database or payment actions.
const fs=require('fs'), vm=require('vm'), assert=require('assert'), path=require('path');
const root=path.resolve(process.argv[2] || path.join(__dirname, '../StageUp.UI'));
let checks=0;
function ok(condition,label){assert.ok(condition,label); checks++; console.log('OK '+label);}
function environment(url='http://localhost/StageUp/Explorar/ResultadosBusqueda.aspx') {
    const ids={}, listeners={}, timers=[], all=[];
    let document;
    class Element {
        constructor(tag='div',attrs={}) {this.tagName=tag;this.attrs=attrs;this.value='';this.children=[];this.handlers={};this.hidden=false;this.disabled=false;this.checked=false;this.isConnected=true;this.parent=null;this._text='';this.offsetTop=0;this.offsetHeight=20;this.clientHeight=200;this.tabIndex=['button','a','input','select','textarea'].includes(tag)?0:-1;const classes=new Set();this.classList={add:v=>classes.add(v),remove:v=>classes.delete(v),contains:v=>classes.has(v),toggle:(v,state)=>{const next=state===undefined?!classes.has(v):state;if(next)classes.add(v);else classes.delete(v);return next;}};all.push(this);if(attrs.id)ids[attrs.id]=this;}
        get href(){return new URL(this.attrs.href||'',url).href;} set href(v){this.attrs.href=v;}
        set textContent(v){this._text=v;this.children=[];} get textContent(){return this._text;}
        set innerHTML(v){ // Only the fixed confirmation template needs HTML parsing here.
            this.children=[];
            const section=new Element('section',{'class':'stageup-confirm-dialog'});this.appendChild(section);
            section.appendChild(new Element('h2',{id:'stageup-confirm-title'}));section.appendChild(new Element('p',{id:'stageup-confirm-message'}));
            section.appendChild(new Element('button',{'class':'stageup-confirm-cancel','data-stageup-confirm-cancel':''}));section.appendChild(new Element('button',{'data-stageup-confirm-accept':''}));
        }
        setAttribute(k,v){this.attrs[k]=String(v);if(k==='tabindex')this.tabIndex=+v;} getAttribute(k){return this.attrs[k]??null;} hasAttribute(k){return k in this.attrs;} removeAttribute(k){delete this.attrs[k];}
        appendChild(child){child.parent=this;this.children.push(child);return child;}
        contains(child){return child===this||this.children.some(c=>c.contains(child));}
        matches(s){if(s===':disabled')return this.disabled;return s.split(',').some(p=>{p=p.trim();if(p.startsWith('.'))return (this.attrs.class||'').split(' ').includes(p.slice(1))||this.classList.contains(p.slice(1));if(p==='[hidden]')return this.hidden;if(p.startsWith('['))return this.hasAttribute(p.slice(1,-1));return p===this.tagName;});}
        closest(s){return this.matches(s)?this:this.parent?this.parent.closest(s):null;}
        querySelectorAll(s){const out=[];for(const child of this.children){if(child.matches(s))out.push(child);out.push(...child.querySelectorAll(s));}return out;}
        querySelector(s){return this.querySelectorAll(s)[0]||null;}
        addEventListener(n,f){(this.handlers[n]??=[]).push(f);}
        fire(n,event={}){const e={currentTarget:this,target:this,preventDefault(){this.defaultPrevented=true;},stopPropagation(){},...event};for(const f of this.handlers[n]||[])f(e);return e;}
        click(){this.fire('click');} focus(){document.activeElement=this;for(const f of listeners.focusin||[])f({target:this});}
        scrollIntoView(){} getClientRects(){return this.closest('[hidden]')?[]:[{}];}checkValidity(){return !this.invalid;}reportValidity(){this.reported=true;}
    }
    const body=new Element('body');
    document={body,activeElement:body,querySelector:s=>body.querySelector(s),querySelectorAll:s=>body.querySelectorAll(s),getElementById:id=>ids[id]||null,createElement:t=>new Element(t),createTextNode:t=>({textContent:t}),addEventListener:(n,f)=>(listeners[n]??=[]).push(f)};
    const window={location:{href:url,pathname:new URL(url).pathname,search:new URL(url).search,origin:new URL(url).origin},URLSearchParams,requestAnimationFrame:f=>f(),setTimeout:f=>{timers.push(f);return timers.length;},clearTimeout:id=>{if(id)timers[id-1]=null;},getComputedStyle:()=>({visibility:'visible'})};
    const ctx=vm.createContext({window,document,URL,URLSearchParams,Event:function(type){this.type=type;},console});
    function load(name){vm.runInContext(fs.readFileSync(path.join(root,'Scripts',name),'utf8'),ctx);}
    function fn(name,next){const source=fs.readFileSync(path.join(root,'Scripts/site.js'),'utf8');const start=source.indexOf('    function '+name+'()');const end=next?source.indexOf('    function '+next+'()',start):source.indexOf('    setupNavigation();',start);vm.runInContext(source.slice(start,end)+'\n'+name+'();',ctx);}
    function emit(n,e){e.preventDefault=()=>e.defaultPrevented=true;for(const f of listeners[n]||[])f(e);return e;}
    return {Element,document,window,body,ids,ctx,load,fn,emit,flush:()=>{while(timers.length){const f=timers.shift();if(f)f();}},add:(tag,attrs,parent=body)=>parent.appendChild(new Element(tag,attrs))};
}
function filters(query='?q=sala&ubicacion=Palermo&precioMax=10000&equip=SONIDO,ESPEJOS&orden=PrecioMenor') {
    const e=environment('http://localhost/StageUp/Explorar/ResultadosBusqueda.aspx'+query), {add,document}=e;
    const panel=add('aside',{'data-filter-panel':''});panel.hidden=true;
    const backdrop=add('div',{'data-filter-backdrop':''});backdrop.hidden=true;
    const region=add('div',{'data-active-filters':''}),list=add('div',{'data-active-filter-list':''},region);
    const clear=add('button',{'data-clear-filters':''},region),reset=add('button',{'data-reset-filter-fields':''},panel),apply=add('button',{'data-apply-filters':''},panel),close=add('button',{'data-filter-close':''},panel);
    const names=['type','location','province','city','zone','capacity','price-min','price','availability','time-from','time-to','rating','floor','features'];
    for(const name of names)add('input',{id:'filter-'+name},panel);
    for(const code of ['SONIDO','ESPEJOS']){const el=add('input',{'data-equip-code':code},panel);el.value=code;}
    const quick=add('button',{'data-filter-target':'filter-price','data-filter-params':'precioMin,precioMax'});quick.textContent='Precio';
    add('button',{'data-filter-open':''});const search=add('input',{'data-results-search-input':''});search.value='sala';
    e.load('modal-focus.js');e.fn('setupFilters','setupComparisonBar');
    return {...e,panel,list,region,clear,reset,apply,close,quick};
}
function filterChecks(){
    let e=filters();
    ok(e.list.children.length===4,'render applied location, price and two equipment chips');
    e.quick.click();ok(!e.panel.hidden&&e.document.activeElement===e.ids['filter-price'],'quick price opens the real field');
    e.reset.click();ok(e.ids['filter-price'].value===''&&e.list.children.length===4,'reset only edits draft; applied summary stays truthful');
    e.close.click();e.flush();e.quick.click();ok(e.ids['filter-price'].value==='10000','closing without applying preserves previous criteria');
    e.ids['filter-price'].value='2500';e.apply.click();let p=new URL(e.window.location.href,'http://localhost').searchParams;
    ok(p.get('precioMax')==='2500'&&p.get('ubicacion')==='Palermo'&&p.get('q')==='sala'&&p.get('orden')==='PrecioMenor','apply combines fields and keeps query/order');
    e=filters();e.list.children[0].click();p=new URL(e.window.location.href,'http://localhost').searchParams;
    ok(!p.has('ubicacion')&&p.get('precioMax')==='10000','remove location updates URL without dropping price');
    e=filters();e.list.children[2].click();p=new URL(e.window.location.href,'http://localhost').searchParams;
    ok(p.get('equip')==='ESPEJOS','remove one equipment preserves the other');
    e=filters();e.clear.click();p=new URL(e.window.location.href,'http://localhost').searchParams;
    ok(p.size===2&&p.get('q')==='sala'&&p.get('orden')==='PrecioMenor','clear applied filters updates URL and preserves search/order');
    e=filters();e.quick.click();e.ids['filter-price'].invalid=true;e.apply.click();ok(e.ids['filter-price'].reported&&e.window.location.href.includes('precioMax=10000'),'invalid draft does not navigate');
    e=filters('');ok(e.region.hidden&&!e.quick.classList.contains('has-value'),'no criteria means no misleading applied chips');
}
function focusChecks(){
    const e=environment(),{add,document,window}=e,trigger=add('button'),outside=add('button'),panel=add('section'),a=add('button',{},panel),disabled=add('button',{},panel),b=add('button',{},panel);disabled.disabled=true;
    e.load('modal-focus.js');const f=window.StageUpModalFocus;f.activate(panel,trigger);
    b.focus();ok(e.emit('keydown',{key:'Tab',shiftKey:false}).defaultPrevented&&document.activeElement===a,'Tab wraps from last to first');
    a.focus();ok(e.emit('keydown',{key:'Tab',shiftKey:true}).defaultPrevented&&document.activeElement===b,'Shift+Tab wraps to last, skipping disabled');
    outside.focus();ok(document.activeElement===a,'focus cannot escape to page behind dialog');
    const sheet=add('section',{},panel),option=add('button',{},sheet);f.activate(sheet,a);option.focus();e.emit('keydown',{key:'Tab'});ok(document.activeElement===option,'nested options keep their own focus');
    f.deactivate(sheet);sheet.hidden=true;ok(document.activeElement===a&&f.isTop(panel),'closing options restores parent focus');
    const confirmation=add('section'),accept=add('button',{},confirmation);f.activate(confirmation,b);accept.focus();f.deactivate(confirmation);confirmation.hidden=true;
    ok(document.activeElement===b&&f.isTop(panel),'closing separate confirmation returns to parent');
    f.deactivate(panel);ok(document.activeElement===trigger,'closing modal returns to trigger');
    outside.focus();ok(document.activeElement===outside,'page focus is released after close');
    const empty=add('section');f.activate(empty,trigger);e.emit('keydown',{key:'Tab'});ok(document.activeElement===empty,'empty dialog remains keyboard navigable');f.deactivate(empty);
}
async function confirmChecks(){
    const e=environment(),trigger=e.add('button');e.load('modal-focus.js');e.load('stageup-confirm.js');trigger.focus();
    let result=null;let promise=e.window.StageUpConfirm.show({message:'Prueba'}).then(v=>result=v);
    e.document.querySelector('.stageup-confirm-cancel').click();e.flush();await promise;ok(result===false&&e.document.activeElement===trigger,'cancel resolves false and restores focus');
    promise=e.window.StageUpConfirm.show({message:'Prueba'}).then(v=>result=v);e.emit('keydown',{key:'Escape'});e.flush();await promise;ok(result===false,'Escape never confirms action');
    let actions=0;trigger.addEventListener('click',event=>{if(e.window.StageUpConfirm.confirmPostback(trigger,event,'Guardar'))actions++;});
    trigger.click();ok(actions===0,'protected action waits for confirmation');
    const accept=e.document.querySelector('[data-stageup-confirm-accept]');accept.click();accept.click();e.flush();await Promise.resolve();ok(actions===1,'double click on confirm executes the original action exactly once');
    trigger.click();e.document.querySelector('.stageup-confirm-cancel').click();e.flush();await Promise.resolve();ok(actions===1,'cancelled postback does not execute original action');
}
function navigationChecks(){
    for(const [page,back,expected] of [['MisReservas.aspx',null,'MisReservas.aspx'],['DetalleReserva.aspx','SolicitudesRecibidas.aspx','MisEspacios.aspx'],['DetalleReserva.aspx','MisReservas.aspx','MisReservas.aspx'],['DetallePago.aspx','SolicitudesRecibidas.aspx','MisEspacios.aspx'],['DisponibilidadEspacio.aspx',null,'MisEspacios.aspx']]){
        const e=environment('http://localhost/StageUp/'+page),side=e.add('aside',{'class':'app-sidebar'}),toggle=e.add('button',{'data-sidebar-toggle':'','aria-expanded':'false'},side),nav=e.add('nav',{'data-sidebar-navigation':''},side),links=['MisReservas.aspx','MisEspacios.aspx'].map(p=>e.add('a',{href:p},nav));
        const originalAll=e.document.querySelectorAll;e.document.querySelectorAll=s=>s==='.sidebar-navigation a,.primary-navigation a,.user-profile-link'?links:originalAll(s);
        const originalOne=e.document.querySelector;const backLink=back?e.add('a',{href:back}):null;e.document.querySelector=s=>s.startsWith('a[id$=')?backLink:originalOne(s);
        e.fn('setupAccountNavigation');ok(links.find(l=>l.href.endsWith(expected)).classList.contains('is-active'),'section marked for '+page+' '+(back||''));
        toggle.click();ok(side.classList.contains('is-open')&&toggle.getAttribute('aria-expanded')==='true','account menu opens');
        side.fire('keydown',{key:'Escape'});ok(!side.classList.contains('is-open')&&e.document.activeElement===toggle,'Escape closes account menu and restores focus');
    }
    for(const [page,parent] of [['GestionGruposPermisos.aspx','GestionRoles.aspx'],['ConfigurarIdioma.aspx','GestionIdiomas.aspx'],['DetalleOperacionFinanciera.aspx','HistorialFinanciero.aspx']]){
        const e=environment('http://localhost/StageUp/Interno/'+page),input=e.add('input',{'data-internal-menu-search':''}),list=e.add('div',{'data-internal-menu-list':''}),empty=e.add('p',{'data-internal-menu-empty':''}),link=e.add('a',{'class':'internal-nav-link',href:parent,'data-menu-name':'Gestión de roles'},list);
        e.load('internal-menu.js');ok(link.getAttribute('aria-current')==='location','internal parent marked for '+page);
        input.value='gestion';input.fire('input');ok(!link.hidden&&empty.hidden,'menu search remains accent insensitive');
    }
}
(async()=>{filterChecks();focusChecks();await confirmChecks();navigationChecks();console.log('PASSED '+checks+' checks');})().catch(e=>{console.error(e);process.exitCode=1;});
