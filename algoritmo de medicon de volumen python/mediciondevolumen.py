from ctypes.wintypes import PCHAR
from lib2to3.pytree import Base
from pickle import TRUE
from tkinter import ROUND, W
from unittest import TextTestResult
import h5py
import numpy as np
from vedo import *
import vedo
import vista
from pymesh import *
import pymesh as pm
import vtk
from vtk import *
from scipy import interpolate
from scipy.spatial import ConvexHull, Voronoi, voronoi_plot_2d,Delaunay,procrustes
from scipy.interpolate import griddata
import matplotlib.pyplot as plt
import time
from sklearn.cluster import DBSCAN



def norm_pc1(pc):

    min_x = np.min(pc[:, 0])
    min_y = np.min(pc[:, 1])
    min_z = np.min(pc[:, 2])

    pc[:, 0] = pc[:, 0] - min_x
    pc[:, 1] = pc[:, 1] - min_y
   

    return pc
#---------------------------------------------------------------------------------------
#---------------------------------------------------------------------------------------
def norm_pc(pc):

    min_x = np.min(pc[:, 0])
    min_y = np.min(pc[:, 1])
    min_z = np.min(pc[:, 2])

    pc[:, 0] = pc[:, 0] - min_x
    pc[:, 1] = pc[:, 1] - min_y
    pc[:, 2] = pc[:, 2] - min_z

    return pc
dim_x = 606
dim_y = 888
dim_z = 444
#------------------------------obtencion de la superficie de volumen en el recipiente----------------
def resta_pc(pcres):
   
    res=np.max(pcres[:,2]) 
    a=res*0.8
    
   
    seleccion=pcres[abs(pcres[:,2])>a]
    xyz=seleccion[:,:3]
    seleccion=resta_valores_atipicos(seleccion,3)
    vista_volumen=vista.vista([1,1])
    mesh_volumen=Points(seleccion)
        
    vista_volumen.graficar(mesh_volumen)
   
    return seleccion
#---------------------------------------------------------------------------------------
#---------------------------------------------------------------------------------------
def Creacion_de_puntos_aleatorios(pc):
    ind_puntos = np.arange(0, pc.shape[0])
    puntos_aleatorios = np.random.choice(ind_puntos, size=2048, replace=False)
    pc_dwn = pc[puntos_aleatorios, :]
    return (pc_dwn)
#---------------------------------------------------------------------------------------
#---------------------------------------------------------------------------------------
def suma_pc(rest1):
     k1=np.mean(rest1[:,2]) 
     res=0
     for i in rest1[0]:
        for j in rest1[1]:
            
            res=np.max(np.max(rest1[:,2]))+res
     k=res/len(rest1[2])
     return(k)

#---------------------------------------------------------------------------------------
#---------------------------------------------------------------------------------------
def nube_de_puntos(pc):
      PointCloud = Points(pc, r=3.0)
      scalars = PointCloud.points()[:, 2]
      PointCloud.pointColors(scalars, cmap='magma')
      return (PointCloud)
#---------------------------------------------------------------------------------------
#---------------------------------------------------------------------------------------
def resta_valores_atipicos(pcres,t):
   std=np.std(pcres)#sacar puntuacion
   ##t=0.4
   rest=np.mean(pcres)
   #--------------deteccion de valores atipicos-------------
   for h in pcres:
       salida=[]
       z=(h-rest)/std
       if (np.abs(z.any())>t):
          salida.append(h)
       salida=np.array(salida)
  
       #print("zsalida",z.shape)
       #print("zstd",std)
       #print("salida valores atipicos z",salida)
       #print("media z",rest)
      
#-----------eliminacion de los valores ----------------
   for i in salida:
        pcres=np.delete(pcres,np.where(salida==i))
   print("salida", pcres.shape)

   return pcres
#---------------------------------------------------------------------------------------
#---------------------------------------------------------------------------------------
def completar_vector (v,pcr):
    vi=v[5]
    g=[]
    if len(v)!=len(pcr[:,2]):
        p=[ vi for s in range((len(pcres[:,2])-len(v)))]
        p=np.array(p)
        g=np.append(v,p)
        print("p", len(g))
        pcres[:,2]=g
    return pcr
#---------------------------------------------------------------------------------------------
#----------------------------------------------------------------------------------------------
def puntos_volumen(pcres,vol):
    aux=[]
    pcr=pcres
    #res=suma_pc(vol)
    res=np.max(vol[:,2]) 
    a=res*0.98
    columna = [fila[2] for fila in pcres]
    
    seleccion=pcres[abs(pcres[:,2])>a]
    
    return seleccion
def puntos_fuera_de_caras(puntos, caras):
    # Inicializa una lista para almacenar los índices de los puntos seleccionados
    puntos_seleccionados_indices = []

    # Recorre todos los puntos
    for punto_idx in range(len(puntos)):
        punto_actual = puntos[punto_idx]

        # Comprueba si el punto está dentro de alguna de las caras
        dentro_de_cara = False
        for cara_indices in caras:
            puntos_cara = puntos[cara_indices]
            normal = np.cross(puntos_cara[1] - puntos_cara[0], puntos_cara[2] - puntos_cara[0])
            dist = np.dot(punto_actual - puntos_cara[0], normal) / np.linalg.norm(normal)
            if abs(dist) < 0:  # No se considera el radio
                dentro_de_cara = True
                break

        # Si el punto no está dentro de ninguna cara, lo agrega a la lista de puntos seleccionados
        if not dentro_de_cara:
            puntos_seleccionados_indices.append(punto_idx)

    # Convierte la lista de índices a un arreglo numpy y elimina duplicados
    puntos_seleccionados_indices = np.unique(puntos_seleccionados_indices)

    # Obtiene los puntos seleccionados
    puntos_seleccionados = puntos[puntos_seleccionados_indices]

    return puntos_seleccionados
def filtrar_ruido_dbscan(xyz, epsilon, minPts):
  
    dbscan = DBSCAN(eps=epsilon, min_samples=minPts)
    clusters = dbscan.fit_predict(xyz)

    # Filtrar puntos de ruido
    xyz_filtrado = xyz[clusters != -1]

    return xyz_filtrado



#---------------------------------------------------------------------------------------
#---------------------------------************principal*************------------------------------------------------------
def medicion_volumen(data_xyz):
    with h5py.File('Data/Plato_1.h5', 'r') as hdf:
         ls = list(hdf.keys())
         plt = Plotter(shape=(2, 2))
         #-------------- toma de datos de la imagen de referencia----------------------------------------
         Tapa=np.array(hdf.get(ls[0]))#*0.025

         #bx_pc = PointCloud.box()
         #cm = PointCloud.centerOfMass()
         #-----------creando una matrix con el volumen---------------------
              
        
         prueba= np.array(data_xyz)#*0.025
#-------------------pruebita con seleccion de z por rangos------------------ DESDE AQUI---
         puntos_internos=extraer_puntos_por_z_y_en_rango(prueba)
         puntos_internos=filtrar_ruido_dbscan(puntos_internos,90.9,100)
         #aux=int( len(puntos_internos)/3)
         #pc2=np.array(puntos_internos).reshape(aux,Tapa.shape[1])
         #print('pc2',pc2)
         dif=vedo.Points( puntos_internos ).c("orange")
         vista_volumen=vista.vista([1,1])
         vista_volumen.graficar(dif)

         #tri1 = Delaunay(puntos_internos)
         ## Obtener las caras (índices de los vértices que forman cada triángulo)
         #caras_indices1 = tri1.simplices
         #mesh_volmen=vedo.Mesh([puntos_internos, caras_indices1],c='yellow',alpha=0.1)
         #vista_volumen=vista.vista([1,1])
         #vista_volumen.graficar(mesh_volmen)

         X,Y,Z=interpolacion_puntos(puntos_internos)
         x,y,z=interpolacion_puntos(Tapa)
         #graficarplot(X,Y,Z,'imagen de entrada')
         #graficarplot(x,y,z,'imagen guardada')

         # Crear un objeto de malla fusionada
         malla_fusionada = np.stack((x, y, z), axis=-1)
         malla_fusionada1 = np.stack((X, Y, Z), axis=-1)
         #coordenadas_np = np.array( malla_fusionada)
         #coordenadas_np1 = np.array( malla_fusionada1)
       
         nuevospuntos= convertir_malla_a_vedo(malla_fusionada)
         coordenadas = nuevospuntos.points()
         coordenadas_np = np.array(coordenadas)
         ##coordenadas_np=filtrar_puntos_por_rango(coordenadas_np,Tapa)
         ##coordenadas_np=vedo.Points(coordenadas_np).c("orange")
         ##vista_nuevospuntos=vista.vista([1,1])
         ##vista_nuevospuntos.graficar(coordenadas_np)


         nuevospuntos1= convertir_malla_a_vedo(malla_fusionada1)
         coordenadas1 = nuevospuntos1.points()
         coordenadas_np1 = np.array(coordenadas1)
         ##coordenadas_np1=filtrar_puntos_por_rango(coordenadas_np1,prueba)
         ##coordenadas_np1=vedo.Points(coordenadas_np1).c("orange")
         ##vista_nuevospuntos=vista.vista([1,1])
         ##vista_nuevospuntos.graficar(coordenadas_np1)
         
      
      
         #mesh_volmen=vedo.Mesh(coordenadas_np,c='yellow',alpha=5)
         #vista_volumen=vista.vista([1,1])
         #vista_volumen.graficar(mesh_volmen)

         #mesh_volmen1=vedo.Mesh(coordenadas_np1,c='yellow',alpha=5)
         #vista_volumen=vista.vista([1,1])
         #vista_volumen.graficar(mesh_volmen1)

       
         #print('forma tarro',npZ.shape)
         #print('forma objeto',np1Z.shape)

#------adiciono el objeto segmentado al tarro vacio--------
         pc2=np.append(coordenadas_np1,coordenadas_np)
         aux=int( len(pc2)/3)
         puntos_fusionados=np.array(pc2).reshape(aux,Tapa.shape[1])
         coordenadas=vedo.Points(puntos_fusionados).c("orange")
         vista_nuevospuntos=vista.vista([1,1])
         vista_nuevospuntos.graficar(coordenadas)

         volumen=mallavolumen( puntos_fusionados,coordenadas_np1)
         volumen=np.array(volumen)
         coordenadasvolumen=vedo.Points(volumen).c("orange")
         #vista_nuevospuntos=vista.vista([1,1])
         #vista_nuevospuntos.graficar(coordenadasvolumen)
         #tri1 = Delaunay(Tapa)
        
         ## Obtener las caras (índices de los vértices que forman cada triángulo)
         #caras_indices1 = tri1.simplices
         #mesh_volmen=vedo.Mesh([Tapa, caras_indices1],c='yellow',alpha=0.1)
         mesh_volumen=vedo.Mesh(coordenadasvolumen,c=None,alpha=3)
         vol= vedo.delaunay3D(mesh_volumen, alphaPar=0, tol=None, boundary=False)
         a=vedo.TetMesh.tomesh(vol)
    
         c= Mesh.volume(a)
         vista_volumen.graficar(a)

         print("volumen ocupado",c)

def interpolacion_puntos(nube_puntos):

        x_points=nube_puntos[:,0]
        y_points=nube_puntos[:,1]
        z_points=nube_puntos[:,2]

            # Definir la resolución de la malla en la que se hará la interpolación
        resolucion_x = 50
        resolucion_y = 50

        # Generar una malla uniforme para la interpolación
        malla_x, malla_y = np.meshgrid(np.linspace(min(x_points), max(x_points), resolucion_x), np.linspace(min(y_points), max(y_points), resolucion_y))
                                   

        # Realizar la interpolación de los puntos
        malla_z = griddata((x_points, y_points), z_points, (malla_x, malla_y), method='linear')
        cantidad_nan = np.sum(np.isnan(malla_z))
        malla_z_sin_nan = malla_z.copy()  # Creamos una copia de la malla Z
        nan_indices = np.isnan(malla_z_sin_nan)  # Encontrar los índices de los valores NaN
        malla_z_sin_nan[nan_indices] = np.nanmax(malla_z_sin_nan)
        malla_z=malla_z_sin_nan
     
        return malla_x,malla_y,malla_z

def graficarplot(malla_x,malla_y,malla_z,nombre):

        print('********************************************************************************')
       
        #print("Datos de malla_x:")
        #print(malla_x)

        ## Imprimir los datos de malla_y
        #print("Datos de malla_y:")
        #print(malla_y)

        ## Imprimir los datos de malla_z
        #print("Datos de malla_z:")
        #print(malla_z)

     # Crear una figura
        fig = plt.figure(num=nombre,figsize=(8, 6))

        # Crear un subplot para la visualización de la malla interpolada
        ax = fig.add_subplot(111, projection='3d')

        # Visualizar la malla interpolada
        ax.plot_surface(malla_x, malla_y, malla_z, cmap='viridis', edgecolor='none')
        

        # Trazar los ejes de coordenadas
    ## Añadir etiquetas a los ejes
        ax.set_xlabel('Eje X')
        ax.set_ylabel('Eje Y')
        ax.set_zlabel('Eje Z')

    #    # Añadir leyenda
    #    ax.legend()
        ## Añadir los puntos de la nube original
        #puntos=ax.scatter(nube_puntos[:, 0], nube_puntos[:, 1], nube_puntos[:, 2], color='red')
        plt.show()

def convertir_malla_a_vedo(malla_fusionada):

 
    X = malla_fusionada[:, :, 0].flatten()
    Y = malla_fusionada[:, :, 1].flatten()
    Z = malla_fusionada[:, :, 2].flatten()

    # Crear un objeto vedo.Points a partir de las coordenadas
    puntos_vedo = vedo.Points([X, Y, Z]).c("orange")  # El método .c() asigna un color a los puntos
    
    return puntos_vedo
       
def filtrar_puntos_por_rango(coordenadas_np, data_xyz):
  

    # Obtener los rangos mínimos y máximos de cada coordenada
    rango_x = (np.min(data_xyz[:, 0])+50, np.max(data_xyz[:, 0])-5)
    rango_y = (np.min(data_xyz[:, 1])+50, np.max(data_xyz[:, 1])-20)
    rango_z = (np.min(data_xyz[:, 2]), np.max(data_xyz[:, 2])+30)

    # Filtrar los puntos dentro del rango especificado
    filtro_x = np.logical_and(coordenadas_np[:, 0] >= rango_x[0], coordenadas_np[:, 0] <= rango_x[1])
    filtro_y = np.logical_and(coordenadas_np[:, 1] >= rango_y[0], coordenadas_np[:, 1] <= rango_y[1])
    filtro_z = np.logical_and(coordenadas_np[:, 2] >= rango_z[0], coordenadas_np[:, 2] <= rango_z[1])

    # Aplicar los filtros a los datos
    datos_filtrados = coordenadas_np[np.logical_and.reduce((filtro_x, filtro_y, filtro_z))]

    return datos_filtrados

def extraer_puntos_por_z_y_en_rango(array):
    # Crear un array para almacenar los puntos dentro del rango para cada valor de z
    puntos_por_z_y_en_rango = []

    # Iterar sobre los valores únicos de z
    for z in np.unique(array[:, 2]):
        # Filtrar los puntos donde z es igual al valor actual
        puntos_z = array[array[:, 2] == z, :2]  # Seleccionar solo las columnas de x e y para este z

        # Calcular los mínimos y máximos de x e y para este z
        min_x, max_x = np.min(puntos_z[:, 0]), np.max(puntos_z[:, 0])
        min_y, max_y = np.min(puntos_z[:, 1]), np.max(puntos_z[:, 1])

        # Filtrar los puntos dentro del rango especificado para este z
        filtro_x = np.logical_and(puntos_z[:, 0] > min_x+80, puntos_z[:, 0] < max_x-80)
        filtro_y = np.logical_and(puntos_z[:, 1] > min_y+80, puntos_z[:, 1] < max_y-80)
        puntos_en_rango = puntos_z[np.logical_and(filtro_x, filtro_y)]

        # Agregar los puntos dentro del rango para este valor de z, con los valores de z originales
        puntos_por_z_y_en_rango.append(np.column_stack((puntos_en_rango, np.full_like(puntos_en_rango[:, :1], z))))

    # Concatenar los puntos en un solo array
    puntos_por_z_y_en_rango = np.concatenate(puntos_por_z_y_en_rango)

    return puntos_por_z_y_en_rango

def mallavolumen(puntos_combinados,puntosz):
   puntos=[]
   maxZ=np.max(puntos_combinados[:,2])
   for indice in range(len(puntosz)):
       a=puntos_combinados[indice,2]
       b=puntosz[indice,2]
       if  a<=b and a!=maxZ:
           puntos.append(puntos_combinados[indice])
   return puntos    

#   return puntos
#def mallavolumen(puntos_combinados, puntosz):
#    puntos = []
#    for punto_z in puntosz:
#        for punto_combinado in puntos_combinados:
#            if punto_combinado[2] <= punto_z:
#                puntos.append(punto_combinado)
#    return puntos
