import asyncio
import cv2
import pydobot
import time
import numpy as np

# 서버 정보
SERVER_IP = '10.10.20.105'
SERVER_PORT = 12345
MACHINE_ID = "1"  # 기계 ID
MACHINE_CATEGORY = "피규어"  # 기계 카테고리

# 로봇팔 초기 좌표
x1, y1, z1 = 235, 0, 118
device = None
camera = None  # USB 웹캠 객체
streaming_active = False  # 웹캠 스트리밍 활성화 상태


# Dobot 초기 설정
async def MachineSetting():
    global device
    port = 'COM5'
    device = pydobot.Dobot(port=port, verbose=True)
    device.move_to(x1, y1, z1, 0)
    print("Dobot Initialized.")


# 웹캠 초기화
async def initialize_webcam():
    global camera
    camera = cv2.VideoCapture(0)  # 0번 카메라 사용
    if not camera.isOpened():
        raise Exception("웹캠 초기화 실패")
    print("Webcam Initialized.")


# 서버와 연결
async def connect_to_server():
    print(f"Connecting to server {SERVER_IP}:{SERVER_PORT}...")
    reader, writer = await asyncio.open_connection(SERVER_IP, SERVER_PORT)
    print("Connected to server.")
    return reader, writer


# 데이터 전송 함수
async def send_message(writer, act_type, sender_id="", message="", binary_data=None):
    try:
        # 메시지 본문 (UTF-8 인코딩)
        if binary_data is not None:  # 바이너리 데이터 전송
            body_bytes = binary_data
        else:
            body_bytes = message.encode('utf-8') if message else b""

        # 헤더 생성 (128바이트 고정 크기)
        header = f"{act_type}/{sender_id}/{len(body_bytes)}"
        header_bytes = header.ljust(128, '\0').encode('utf-8')  # 128바이트 고정 길이로 패딩

        # 헤더와 본문 결합
        full_data = header_bytes + body_bytes

        # 데이터 전송
        writer.write(full_data)
        await writer.drain()
        print(f"Data sent: Header={header}, Body={len(body_bytes)} bytes")
    except Exception as e:
        print(f"Error sending message: {e}")


# 데이터 수신 함수
async def receive_message(reader):
    try:
        if reader.at_eof():
            print("Reader at EOF")
            return None, None, None

        # 1. 헤더 수신 (128바이트)
        header_bytes = await reader.readexactly(128)
        header = header_bytes.decode('utf-8').strip('\0')
        parts = header.split('/')
        act_type = int(parts[0])
        sender_id = parts[1]
        data_length = int(parts[2])

        print(f"Header received: Type={act_type}, SenderID={sender_id}, DataLength={data_length}")

        # 2. 본문 데이터 수신
        body = ""
        if data_length > 0:
            body_bytes = await reader.readexactly(data_length)
            body = body_bytes.decode('utf-8')
            print(f"Body received: {body}")

        return act_type, sender_id, body
    except asyncio.IncompleteReadError:
        print("Incomplete read error.")
        return None, None, None
    except Exception as e:
        print(f"Error receiving message: {e}")
        return None, None, None


# 웹캠 이미지 캡처 및 서버로 전송
async def send_webcam_image(writer):
    global camera, streaming_active
    try:
        while streaming_active:  # 스트리밍이 활성화된 동안 반복
            loop = asyncio.get_running_loop()

            # 웹캠에서 이미지 캡처 (비동기로 처리)
            ret, frame = await loop.run_in_executor(None, camera.read)
            if not ret:
                print("웹캠에서 이미지를 캡처할 수 없습니다.")
                continue

            # 이미지를 JPEG 형식으로 인코딩 (비동기로 처리)
            _, jpeg_image = await loop.run_in_executor(None, cv2.imencode, '.jpg', frame)
            binary_image = jpeg_image.tobytes()

            # 서버로 전송
            await send_message(writer, act_type=5, sender_id=MACHINE_ID, binary_data=binary_image)

            # 잠시 대기 (프레임 전송 속도 조정)
            await asyncio.sleep(0.1)
    except asyncio.CancelledError:
        print("Webcam image task cancelled.")
    except Exception as e:
        print(f"Error capturing and sending webcam image: {e}")


# 명령 실행 함수 (기존 명령 중단 가능)
async def execute_command(command):
    try:
        await command()
    except Exception as e:
        print(f"Error executing command: {e}")


# 로봇팔 클라이언트 루프
async def robot_client():
    global streaming_active
    await MachineSetting()  # 로봇팔 초기 설정
    await initialize_webcam()  # 웹캠 초기화

    # 서버와 연결
    reader, writer = await connect_to_server()

    # 서버에 기계 ID와 카테고리 전송
    await send_message(writer, act_type=2, sender_id=MACHINE_ID, message=MACHINE_CATEGORY)

    # 웹캠 스트리밍 작업
    webcam_task = None

    try:
        while True:
            # 서버에서 명령 수신
            if not reader.at_eof():
                act_type, sender_id, body = await receive_message(reader)
                if act_type is None:
                    continue

                # 명령 처리
                if act_type == 9 and body == "Request":  # Streaming 요청
                    streaming_active = True
                    print("Starting webcam streaming...")

                    # 웹캠 스트리밍 작업 시작
                    if webcam_task is None or webcam_task.done():
                        webcam_task = asyncio.create_task(send_webcam_image(writer))

                elif act_type == 8:  # GameOut (스트리밍 종료 요청)
                    streaming_active = False
                    print("Stopping webcam streaming...")
                    if webcam_task:
                        webcam_task.cancel()
                        try:
                            await webcam_task
                        except asyncio.CancelledError:
                            print("Webcam streaming task cancelled.")

                elif act_type == 7:  # 로봇팔 이동 명령
                    if body == "Up":
                        await execute_command(MachineUp)
                    elif body == "Down":
                        await execute_command(MachineDown)
                    elif body == "Left":
                        await execute_command(MachineLeft)
                    elif body == "Right":
                        await execute_command(MachineRight)
                    elif body == "Front":
                        await execute_command(MachineFront)
                    elif body == "Behind":
                        await execute_command(MachineBehind)
                    elif body == "GripOpen":
                        await execute_command(MachineGripOpen)
                    elif body == "GripClose":
                        await execute_command(MachineGripClose)
                    elif body == "MoveHome":
                        await execute_command(MoveToHome)
                    elif body == "Grab":
                        await execute_command(GrabSequence)

                elif act_type == 3:  # 종료 명령
                    print("Server requested disconnection.")
                    break

            await asyncio.sleep(0.1)  # CPU 점유율 과다 방지

    except asyncio.CancelledError:
        print("Client loop cancelled.")
    except Exception as e:
        print(f"Error in client loop: {e}")
    finally:
        if webcam_task:
            webcam_task.cancel()
            try:
                await webcam_task
            except asyncio.CancelledError:
                print("Webcam streaming task cancelled.")

        writer.close()
        await writer.wait_closed()
        print("Disconnected from server.")


# 인형 뽑기 동작
async def GrabSequence():
    global z1

    print("Starting Grab Sequence...")

    # 1. 집게 펼치기
    print("Step 1: Gripper Opening...")
    await MachineGripOpen()
    await asyncio.sleep(0.5)

    # 2. 아래로 이동
    target_z = -30
    print(f"Step 2: Moving down to z={target_z}...")
    device.move_to(x1, y1, target_z, 0, wait=True)
    z1 = target_z
    await asyncio.sleep(1)

    # 3. 집게 닫기
    print("Step 3: Gripper Closing...")
    await MachineGripClose()
    await asyncio.sleep(1)

    # 4. 위로 이동
    target_z = 235
    print(f"Step 4: Moving up to z={target_z}...")
    device.move_to(x1, y1, target_z, 0)
    z1 = target_z
    await asyncio.sleep(1)

    # 5. 박스 위치 이동
    print("Step 5: Moving to Box Position...")
    await MoveToBox()
    await asyncio.sleep(1)

    # 6. 마지막 큐 초기화 및 집게 열기
    print("Step 6: Finalizing sequence...")
    await MachineGripOpen()
    await asyncio.sleep(2)

    # 7. 홈 위치 이동
    print("Step 7: Moving to Home Position...")
    await MoveToHome()
    await asyncio.sleep(1)

    print("Grab Sequence Complete!")


# 로봇팔 동작 함수들
async def MachineDown():
    global z1
    z1 -= 10
    device.move_to(x1, y1, z1, 0)
    print(f"Moved Down: z={z1}")


async def MachineUp():
    global z1
    z1 += 10
    device.move_to(x1, y1, z1, 0)
    print(f"Moved Up: z={z1}")


async def MachineRight():
    global y1
    y1 -= 10
    device.move_to(x1, y1, z1, 0)
    print(f"Moved Right: y={y1}")


async def MachineLeft():
    global y1
    y1 += 10
    device.move_to(x1, y1, z1, 0)
    print(f"Moved Left: y={y1}")


async def MachineFront():
    global x1
    x1 += 10
    device.move_to(x1, y1, z1, 0)
    print(f"Moved Front: x={x1}")


async def MachineBehind():
    global x1
    x1 -= 10
    device.move_to(x1, y1, z1, 0)
    print(f"Moved Behind: x={x1}")


async def MachineGripClose():
    device.grip(True)
    print("Gripper Closed")


async def MachineGripOpen():
    device.grip(False)
    print("Gripper Opened")


async def MoveToHome():
    global x1, y1, z1
    print("Moving to Home Position...")
    device.move_to(235, 0, 118, 0)
    x1, y1, z1 = 235, 0, 118
    print("Moved to Home Position.")

async def MoveToBox():
    global x1, y1, z1
    print("Moving to Box Position...")
    device.move_to(235, -190, 160, 0)
    x1, y1, z1 = 235, -190, 160
    print("Moved to Box Position.")

# 메인 실행
if __name__ == "__main__":
    try:
        asyncio.run(robot_client())
    except KeyboardInterrupt:
        print("Program terminated by user.")
