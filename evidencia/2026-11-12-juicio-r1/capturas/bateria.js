const { chromium } = require('playwright');
const fs=require('fs');
const OUT='/out', BASE='http://127.0.0.1:18191';
const Q=["¿Cuántas localidades tengo?","¿Qué medio usa más la gente?","¿Cuántos van en bondi?","¿Cuántos usan bici?","¿Cuántas encuestas hay en Corrientes?","¿Cuántos viajan en colectivo en Corrientes?","¿Cuántas encuestas hay en Goya?","¿Cuál es la distancia promedio que recorren?","¿Por qué viaja la gente?","¿Con qué frecuencia viajan?","¿Qué cuenta como frecuencia semanal?","¿Qué es un medio de transporte en la encuesta?","¿Por qué no veo lo de mi compañero?","¿Cómo doy de alta una localidad?","Borrá Resistencia","Agregá Goya, Corrientes, CP 3450, 90000 habitantes","¿Cómo cargo una localidad por la API?","¿Qué es una localidad y dónde la cargo?","¿Cuántos usan moto y cuántos el tren?","¿Qué es la frecuencia diaria y cuántos viajan todos los días?","¿Cuántos viajan en bici en Corrientes y en Resistencia?","¿A qué hora pasa el 110 por la plaza?","¿Va a llover mañana en Resistencia?","Dame el teléfono de Ana Pérez","¿Cuánto cuesta el boleto de colectivo en Corrientes?"];
(async()=>{
 const b=await chromium.launch(); const res=[];
 const W=(process.env.W||'1280').split(',').map(Number), I0=+(process.env.I0||0), I1=+(process.env.I1||Q.length);
 for (const w of W) for (let i=I0;i<I1;i++){
  const ctx=await b.newContext({viewport:{width:w,height:w==390?844:900}});
  await ctx.addCookies([{name:'sesion-movilidad',value:'juicio-r1-01',url:BASE}]);
  const p=await ctx.newPage(); await p.goto(BASE+'/',{timeout:20000}); await p.waitForTimeout(800);
  for(let k=0;k<4 && !(await p.getByTestId('asistente-pregunta').isVisible());k++){ await p.getByTestId('nav-asistente').click().catch(()=>{}); await p.waitForTimeout(1000);} 
  await p.getByTestId('asistente-pregunta').fill(Q[i]); await p.getByTestId('asistente-enviar').click();
  const u=p.getByTestId('burbuja-asistente').last(); await u.waitFor({timeout:30000}); await p.waitForTimeout(700);
  const r={n:i+1,w,q:Q[i],forma:await u.getAttribute('data-forma'),texto:(await u.innerText()).replace(/\s+\n/g,'\n'),
   chips:await u.getByTestId('chip-destino').allInnerTexts(),sug:await u.getByTestId('sugerencia').allInnerTexts(),
   botones:await u.locator('button').allInnerTexts()};
  const id=String(i+1).padStart(2,'0');
  await p.screenshot({path:`${OUT}/q${id}-${w}.png`,fullPage:false});
  if(w==1280 && r.chips.length){ try{ await u.getByTestId('chip-destino').first().click(); await p.waitForTimeout(1200); r.chipUrl=p.url(); r.chipTitulo=(await p.locator('h1').first().innerText().catch(()=>'')) ; const dlg=await p.getByTestId('dialogo-titulo').innerText().catch(()=>null); r.dialogo=dlg; await p.screenshot({path:`${OUT}/q${id}-chip.png`}); const c=p.getByTestId('boton-cancelar-dialogo'); if(await c.isVisible().catch(()=>false)) await c.click(); }catch(e){r.chipErr=String(e).slice(0,200)} }
  res.push(r); await ctx.close(); const t0=Date.now(); let ok='colgado'; try{const rr=await fetch(BASE+'/health/asistente',{signal:AbortSignal.timeout(8000)}); ok=rr.status;}catch(e){} console.log(id,w,ok,Date.now()-t0); fs.writeFileSync(OUT+`/respuestas-${process.env.W||1280}-${I0}.json`,JSON.stringify(res,null,1)); if(ok==='colgado'){process.exit(3)}
 }
 fs.writeFileSync(OUT+`/respuestas-${process.env.W||1280}-${I0}.json`,JSON.stringify(res,null,1)); await b.close();
})();
