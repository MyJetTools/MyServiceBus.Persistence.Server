
class Main{
    
    
    private static requesting = false;

    private static bodyElement: Element;
    
    static request(){
        
        if (this.requesting)
            return;

        $.ajax({url: '/api/status'})
            .then((r:IStatus) => {

                if (!this.bodyElement)
                    this.bodyElement = document.getElementsByTagName("BODY")[0];

                this.requesting = false;

                this.bodyElement.innerHTML = HtmlRenderer.renderMainContent(r);
                    

            })
            .fail(()=>{
                this.requesting = false;
            });
        
    }
    
    
}

window.setInterval(()=>{
    Main.request();
}, 1000);

Main.request();