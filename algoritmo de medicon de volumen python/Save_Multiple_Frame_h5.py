from arena_api.system import system
from arena_api.enums import PixelFormat
from arena_api.buffer import *
from vedo import *
from pyntcloud import PyntCloud
import ctypes
import numpy as np
import pandas as pd
import h5py
import time
import sys
from mediciondevolumen import medicion_volumen

# Comprueba si la cámara Helios2 es la utilizada
isHelios2 = False


def Crear_Dispositivos():
    '''
    Esta función espera a que el usuario conecte un dispositivo antes de enviar una excepción
        * Número de intentos: 6
        * Tiempo de espera por cada intento: 10 segundos
    '''

    tries = 0
    tries_max = 6
    sleep_time_secs = 10
    while tries < tries_max:
        devices = system.create_device()
        if not devices:
            print(
                f'Intento {tries + 1} de {tries_max}: Esperando por {sleep_time_secs} '
                f'segundos para que un dispositivo sea conectado!')
            for sec_count in range(sleep_time_secs):
                time.sleep(1)
                print(f'Han pasado {sec_count + 1} segundo(s)',
                      '.' * sec_count, end='\r')
            tries += 1
        else:
            print(f'Dispositivos creados: {len(devices)}')
            return devices
    else:
        raise Exception(f'No se han encontrado dispositivos! Conecte un dispositivo y vuelva a ejecutar')


def Validar_Dispositivo(device):
    '''
    Valida si el dispositivo conectado es compatible.
    Si no, probablemente no es una cámara Helios
    '''

    try:
        scan_3d_operating_mode_node = device. \
            nodemap['Scan3dOperatingMode'].value
    except (KeyError):
        print('Nodo Scan3dCoordinateSelector no encontrado. \
                Asegúrese de que el dispositivo sea una cámara Helios.\n')
        sys.exit()

    # Valida si el nodo Scan3dCoordinateOffset existe.
    # Si no, es probable que la cámara Helios tenga un firmware antiguo
    try:
        scan_3d_coordinate_offset_node = device. \
            nodemap['Scan3dCoordinateOffset'].value
    except (KeyError):
        print('Nodo Scan3dCoordinateOffset node no encontrado. \
                Actualice Helios firmware.\n')
        sys.exit()

    # Comprueba si se utiliza la cámara Helios2
    device_model_name_node = device.nodemap['DeviceModelName'].value
    if 'HLT' or 'HPT' in device_model_name_node:
        global isHelios2
        isHelios2 = True


class PointData:
    '''
    Almacenar datos x, y, z e intensidad para un punto dado
    '''

    def __init__(self, x, y, z, intensity):
        self.x = x
        self.y = y
        self.z = z
        self.intensity = intensity


def Separar_Canales(pdata_16bit, total_number_of_channels,
                    channels_per_pixel, inicio_z, rango_z):
    xyz = []

    cont_z = 0

    for i in range(0, total_number_of_channels, channels_per_pixel):
        # Se extraen los diferentes canales por cada punto
        # Compensamos en 1 cada canal dado que pdata_16bit es un entero de 16 bits

        x = pdata_16bit[i]
        y = pdata_16bit[i + 1]
        z = pdata_16bit[i + 2]
        # intensity = pdata_16bit[i + 3]

        #if ((-99999999 < z < 99999999) & (-99999999 < x < 99999999) & (-99999999 < y < 99999999)):
        if ((2000 < z < 3900) & (32050 < x < 33150) & (31900 < y < 33000)):

            x = pdata_16bit[i]
            y = pdata_16bit[i + 1]            

            coor = x, y, z
            xyz.append(coor)

    array_xyz = np.asarray(xyz)

    #columnX = np.min(array_xyz[:, 0])
    
    #columny = np.min(array_xyz[:, 1])
    
    # array_xyz[:, 3] = np.uint8((array_xyz[:, 3] / np.max(array_xyz[:, 3]) * 255))

    # array_xyz = array_xyz[array_xyz[:, 2] > 2600]
    # array_xyz = array_xyz[array_xyz[:, 2] < 3600]

    # array_xyz[:,2] =

    return array_xyz


def buttonfunc():
    global cont_frames
    global cap
    # bu.switch()  # change to next status

    cont_frames += 1
    print("Frame:", cont_frames)

    cap = True

    # return True


def Iniciar(label):
    global cap

    inicio_z = 2000
    rango_z = 500

    # Crear dispositivo
    devices = Crear_Dispositivos()
    device = devices[0]
    print(f'Dispositivo a utilizar:\n\t{device}')

    Validar_Dispositivo(device)

    # Obtener mapa de nodos de transmisión del dispositivos
    tl_stream_nodemap = device.tl_stream_nodemap

    # Habilitar stream auto negotiate packet size
    tl_stream_nodemap['StreamAutoNegotiatePacketSize'].value = True

    # Habilitar stream packet resend
    tl_stream_nodemap['StreamPacketResendEnable'].value = True

    tl_stream_nodemap["StreamBufferHandlingMode"].value = "NewestOnly"

    # Establecer configuración de los nodos ---------------------------------------------
    print('\nConfiguración de nodos:')
    nodemap = device.nodemap

    ConfidenceThresholdMin = 1

    pixel_format = PixelFormat.Coord3D_ABCY16
    nodemap.get_node('PixelFormat').value = pixel_format
    nodemap['Scan3dOperatingMode'].value = 'Distance1250mmSingleFreq'
    nodemap['Scan3dConfidenceThresholdEnable'].value = True
    nodemap['Scan3dConfidenceThresholdMin'].value = ConfidenceThresholdMin

    print(f'\t* Formato de píxel en: Coord3D_ABCY16')
    print(f'\t* Rango de operación 3D: 1.25m')
    print(f'\t* ConfidenceThresholdMin: {ConfidenceThresholdMin}')

    # plt = Plotter()

    prev_frame_time = 0

    # Comenzar la transmisión.
    with device.start_stream():
        with h5py.File('Data/' + label + '.h5', 'w') as hdf:
            while True:
            #for i in range (30):
                curr_frame_time = time.time()

                buffer = device.get_buffer()

                item = BufferFactory.copy(buffer)
                device.requeue_buffer(buffer)

                # Los formatos de píxeles "Coord3D_ABCY16s" y "Coord3D_ABCY16" tienen 4
                # canales por píxel. Cada canal es de 16 bits y representan:
                # - posición x
                # - posición y
                # - posición z
                # - intensidad

                channels_per_pixel = int(item.bits_per_pixel / 16)
                total_number_of_channels = item.width * item.height * channels_per_pixel

                # Buffer.pdata es un puntero (uint8, ctypes.c_ubyte).
                # Este formato de píxel tiene 4 canales, y cada canal es de 16 bits.
                # Es más fácil trabajar con Buffer.pdata si se convierte a 16 bits
                # para que cada valor de canal se lea correctamente.

                pdata_as_uint16 = ctypes.cast(item.pdata, ctypes.POINTER(ctypes.c_uint16))

                array_xyz = Separar_Canales(pdata_as_uint16,
                                            total_number_of_channels,
                                            channels_per_pixel, inicio_z, rango_z)

                fps = str(1 / (curr_frame_time - prev_frame_time))

                PointCloud = Points(array_xyz, r=4.0)
                scalars = PointCloud.points()[:, 2]
                PointCloud.pointColors(scalars, cmap='inferno')

                num_frame = Text2D("Frames capturdos: " + str(cont_frames) + "   ", s=0.9)
                num_frame.alpha(1.0)


                plt.show(PointCloud, num_frame, axes=1, bg='white',
                         title="Stream", interactive=0, zoom=1)

                if cap:
                    medicion_volumen(array_xyz)
                    hdf.create_dataset(label + '-' + str(cont_frames), data=array_xyz)

                    print("Se ha capturado el frame:", cont_frames)

                    d = {'x': array_xyz[:, 0], 'y': array_xyz[:, 1], 'z': array_xyz[:, 2]}

                    cloud = PyntCloud(pd.DataFrame(data=d))
                    cloud.to_file(label + "_" + str(cont_frames) + ".ply")

                    cap = False

                # print(fps)

                BufferFactory.destroy(item)

                prev_frame_time = curr_frame_time

            device.stop_stream()

    system.destroy_device()
    print('Se destruyeron todos los dispositivos creados')


if __name__ == '__main__':
    print('\nADVERTENCIA:\nEste script puede modificar las configuraciones específicas de la cámara!\n')

    cont_frames = 0
    cap = False

    labels = ["Plato_1", "Plato_2", "Plato_3", "Plato_4"]
    print("Seleccione la etiqueta que se va a utilizar:")
    for i, lbl in enumerate(labels):
        print(i, ":", lbl)

    ind_label = 1 #input()

    label = labels[int(ind_label)]

    print("Se seleccinó la etiqueta:", label)

    plt = Plotter(shape=(1, 1))


    bu = plt.addButton(
        buttonfunc,
        pos=(0.7, 0.05),  # x,y fraction from bottom left corner
        states=["Capturar", "Capturar"],
        c=["w", "w"],
        bc=["dg", "dv"],  # colors of states
        font="corobel",  # arial, courier, times
        size=25,
        bold=False,
        italic=False, )

    Iniciar(label)

    print('\nEjecución finalizada')
