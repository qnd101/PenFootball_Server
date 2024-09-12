//fetch player state from Game Server
async function update() {
    for(const card of document.querySelectorAll(".playercard")){
        let id = card.querySelector(".dbid").textContent
        let response = await fetch(serverendpoint+`?id=${id}`)
        let imgname = ""
        let desc = ""

        if(response.ok){
            state = await response.json(); //response에는 state 프로퍼티가 있고, 옵션으로 verses 프로퍼티가 있음 (상대의 id)
            imgname = state.state;
            if(state.verses !== undefined){
                let response2 = await fetch(`/api/users/view?id=${state.verses}`)
                if(response2.ok){
                    let opinfo = await response2.json();
                    desc = `VS ${opinfo.name.length <= 10 ? opinfo.name : opinfo.name.slice(0,7).concat("...")}(${opinfo.rating})`
                }
            }
        }

        let imgelm = card.querySelector("img")

        imgelm.src = imgname === "" ? "" : `/static/images/${imgname}icon.svg`

        let descelm = card.querySelector(".statedesc")
        descelm.textContent = desc;
    }
}

async function recupdate(){
    await update();
    setTimeout(async () => {
        await update();
        await recupdate();
    }, 5000)
}

//1초마다 리더보드 내용을 업데이트 -> 실시간으로 접속 현황 볼 수 있게
recupdate();
