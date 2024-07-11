import vedo 

class vista():

    def __init__(self,size=[1,1]):
        self.P_Frame=vedo.Plotter(shape=(size[0],size[1]))
        
    def graficar(self,Datos,pos=0,title="Ventana"):
        self.P_Frame.show(Datos,bg='black',
                        title="Stream", interactive=3, axes=True, at=pos)