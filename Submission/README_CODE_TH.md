# CatchLab: Sky Catcher — ชุดส่งซอร์สโค้ด

ชุดนี้เป็นเวอร์ชันเกมที่ใช้ UI ของเพื่อน เปิดเล่นผ่าน Unity Web ได้ และรองรับ ESP32-S3, FSR 2 ตัว, จอยสติก 1 ตัว, MQTT, จอ LCD 16×2, Firebase และ Google Sheets

## สิ่งที่อยู่ใน ZIP

| โฟลเดอร์ | หน้าที่ |
| --- | --- |
| `CatchLabFriend/Assets`, `Packages`, `ProjectSettings` | ซอร์สโปรเจกต์ Unity: ฉาก สคริปต์ และ asset ที่ฉากใช้งาน พร้อมไฟล์ `.meta` |
| `ControllerBridge` | เซิร์ฟเวอร์ Node.js รับ MQTT, เปิดเว็บ, ส่งข้อมูลควบคุมให้เกม และบันทึกผลไป Firebase/Google Sheets |
| `Firmware/CatchController` | เฟิร์มแวร์ ESP32-S3 ที่ใช้จริง พร้อมจอ LCD |
| `Firmware/SensorCheck` | สเก็ตช์อ่านค่า ADC เพื่อตรวจ FSR และจอยก่อนใช้งาน |
| `ArduinoLibraries` | สำเนา PubSubClient และ hd44780 พร้อมไฟล์ไลเซนส์/ตัวอย่างของไลบรารี |

ชุดนี้ลดขนาดให้ต่ำกว่า 10 MB: ไม่รวม Web Build ที่ Unity สร้างใหม่ได้, asset ที่ฉากไม่ได้ใช้ และปลั๊กอินของเครื่องพัฒนา เพลงพื้นหลังใน ZIP เป็นท่อนย่อ 75 วินาทีสำหรับเล่นวน; sprite sheet ถูกลดจำนวนสีโดยคงขนาดภาพและข้อมูลตัด sprite เดิม ก่อนเปิดผ่านเว็บต้องสร้าง Web Build ตามขั้นตอนด้านล่าง

ไฟล์ตั้งค่าที่มีรหัสผ่านจริง (`Settings.h`, `.env.local`) และกุญแจ Firebase ไม่รวมใน ZIP ผู้ติดตั้งต้องสร้างจากไฟล์ตัวอย่าง ส่วน `Library`, `Temp`, `Logs`, `node_modules` ของเครื่องผู้พัฒนาไม่รวม เพราะสร้างหรือดาวน์โหลดใหม่ได้ตามขั้นตอนด้านล่าง

## ความตรงกับใบเกณฑ์

| เกณฑ์ในใบคะแนน | จุดที่ตรวจในชุดนี้ |
| --- | --- |
| Web Application จาก Unity | ซอร์สและฉากอยู่ใน `CatchLabFriend/Assets`; สร้าง `Build/Web` จากเมนู **CatchLab Friend > Build Web** |
| เกมรับของ 1–3 แบบ คะแนนต่างกัน | เหรียญทอง +4, เหรียญแดง +10, ถังน้ำมัน +0 และเติมพลัง 20 |
| ความยาก 3 ระดับและเปลี่ยนฉากหลัง | เลือก Easy, Normal, Hard และฉากจากเมนู; ความเร็วและความถี่การตกเปลี่ยนตามระดับ |
| Combo และคำชมเมื่อรับต่อเนื่อง | แสดงเมื่อรับติดกัน 2, 3, 4, 5 ครั้ง; พลาดแล้วเริ่มนับใหม่ |
| ควบคุมซ้าย/ขวาด้วยเซนเซอร์ | โหมด FSR ใช้ตัวซ้าย/ขวา 2 ตัว; โหมด JOYSTICK ใช้แกน Y ของจอย 1 ตัวเลื่อนซ้าย/ขวา เลือกใช้ทีละโหมด |
| ESP32 ส่งข้อมูลทันที | ESP32-S3 ส่งสถานะ `left,right,direction` ผ่าน MQTT ไป Bridge; เกมอ่านผ่าน `/controller/latest` |
| บันทึก User, Score, Time, Level, จำนวนครั้ง | Bridge บันทึกลง Firestore และซิงก์รอบทั้งหมดไปแท็บ `CatchLab Rounds` ใน Google Sheets หากตั้งค่าไว้ |
| วิเคราะห์แนวโน้ม | `/rounds/analytics?playerId=ชื่อผู้เล่น` แสดงประวัติ/สถิติแยก 3 ระดับ; ชีตที่ใช้จริงมี Dashboard เชื่อมข้อมูลรอบ (หากสร้างชีตใหม่ ระบบสร้างแท็บข้อมูล `CatchLab Rounds` ให้ก่อน) |

ใบเกณฑ์ระบุ ESP32 แบบ 38 pin และเซนเซอร์ 2 ชิ้นสำหรับซ้าย/ขวา ชุดนี้ใช้ **ESP32-S3 หนึ่งบอร์ด** กับ FSR 2 ชิ้นในโหมด FSR และจอย 1 ชิ้นในโหมด JOYSTICK ผู้สาธิตควรอธิบายการเลือกโหมดให้ชัด และยืนยันกับอาจารย์หากตรวจจำนวนขาตามรุ่นบอร์ดอย่างเคร่งครัด

## ไลบรารีและเวอร์ชัน

- Unity Editor **6000.5.2f1** และ **Web Build Support**; Unity packages รวมเวอร์ชันใน `CatchLabFriend/Packages/manifest.json` และ `packages-lock.json` (รวม URP, Input System, uGUI, TextMesh Pro ที่มาพร้อมโปรเจกต์)
- Node.js **24.x** กับ npm; ไลบรารีหลักใน `ControllerBridge/package.json`: `aedes 0.51.3`, `firebase-admin 14.3.0`, `google-auth-library 10.9.1`; `package-lock.json` ล็อก dependency ย่อยทั้งหมด
- Arduino IDE กับบอร์ดแพ็กเกจ **esp32 by Espressif Systems 3.3.11**, บอร์ด **ESP32S3 Dev Module**
- Arduino libraries **PubSubClient 2.8** และ **hd44780 1.3.2** (มีสำเนาใน `ArduinoLibraries`; ติดตั้งผ่าน Library Manager หรือคัดลอกไปโฟลเดอร์ Arduino libraries)
- ภาพ เสียง ฟอนต์ และ asset ที่ฉากใช้งานอยู่ใน `CatchLabFriend/Assets`; Unity packages ที่ต้องดาวน์โหลดระบุใน `Packages/manifest.json`

## ติดตั้งและเล่นด้วยคีย์บอร์ด

1. แตก ZIP ให้ `CatchLabFriend`, `ControllerBridge`, `Firmware` อยู่ระดับเดียวกัน
2. เปิด `CatchLabFriend` ใน Unity 6000.5.2f1 แล้วเปิด `Assets/Scenes/SampleScene.unity` กด Play เพื่อทดสอบได้ทันที
3. ถ้าจะเล่นในเว็บ ให้ติดตั้ง **Web Build Support** ใน Unity Hub แล้วเลือกเมนู **CatchLab Friend > Build Web** รอจนสร้าง `CatchLabFriend/Build/Web` เสร็จ
4. ติดตั้ง Node.js 24.x แล้วเปิด Command Prompt ใน `ControllerBridge` รัน `npm.cmd ci` เพื่อติดตั้ง dependency จาก `package-lock.json`
5. รัน `npm.cmd run friend-keyboard` **เพียงหน้าต่างเดียว** แล้วเปิด `http://127.0.0.1:8787/` บนคอมเครื่องเดียวกัน
6. ในเมนูเลือกชื่อผู้เล่น ระดับ ฉาก ภาษา (`TH`/`EN`) และโหมดเซนเซอร์ แล้วกด Play เกมเล่นรอบละ 30 วินาที
7. ถ้าเลือก FSR ให้ใช้ปุ่ม `A`/`D`; ถ้าเลือก JOYSTICK ให้ใช้ลูกศรซ้าย/ขวาแทนเซนเซอร์ในการสาธิตแบบไม่ต่อบอร์ด
8. เมื่อเลิกเล่น ให้กลับไปหน้าต่าง Command Prompt แล้วกด `Ctrl+C` อย่าเปิด Bridge สองหน้าต่างพร้อมกัน เพราะพอร์ต 8787/1883 จะชนกัน

การเล่นด้วยคีย์บอร์ดโดยไม่ตั้งค่า Firebase ยังเปิดเกมได้ แต่การบันทึกผลออนไลน์จะยังไม่สำเร็จจนกว่าจะตั้งค่าตามหัวข้อถัดไป

## ต่อ ESP32-S3 และเล่นด้วยเซนเซอร์

| อุปกรณ์/สัญญาณ | ขา ESP32-S3 | หมายเหตุ |
| --- | --- | --- |
| FSR ซ้าย (เอาต์พุตวงจรแบ่งแรงดัน) | GPIO4 | ADC1 |
| FSR ขวา (เอาต์พุตวงจรแบ่งแรงดัน) | GPIO5 | ADC1 |
| Joystick `VRy` | GPIO6 | ADC1; `VRx` ไม่ใช้ |
| LCD I2C `SDA` | GPIO8 | ตั้งใน `LcdConfig.h` |
| LCD I2C `SCL` | GPIO9 | ตั้งใน `LcdConfig.h` |

ต่อ GND ร่วมกัน ป้อนแรงดันที่เหมาะกับโมดูลและ ADC ของ ESP32-S3 โดยห้ามให้สัญญาณที่ GPIO สูงเกิน 3.3 V หากจอ I2C จ่าย 5 V และ SDA/SCL ถูกดึงขึ้น 5 V ให้ใช้ตัวแปลงระดับสัญญาณก่อนต่อ ESP32-S3

1. คัดลอก `Firmware/CatchController/Settings.example.h` เป็น `Settings.h` แล้วใส่ชื่อ/รหัส Wi‑Fi, IPv4 ของคอมที่รัน Bridge (`BrokerHost`) และรหัส MQTT ใช้ฮอตสปอต/เครือข่ายเดียวกันระหว่างคอมกับบอร์ด
2. ใน Arduino IDE เปิด `Firmware/CatchController/CatchController.ino`, เลือก **ESP32S3 Dev Module** และพอร์ตของบอร์ด อัปโหลดสเก็ตช์นี้ (ไม่ใช่ `SensorCheck.ino` ซึ่งใช้ทดสอบค่าอย่างเดียว)
3. ใน `ControllerBridge` คัดลอก `.env.example` เป็น `.env.local` ใส่ `CATCHLAB_MQTT_HOST` เป็น IPv4 คอม และ `CATCHLAB_MQTT_PASSWORD` ให้ตรงกับ `Settings.h`
4. รัน `npm.cmd run friend` หนึ่งครั้ง เปิด `http://127.0.0.1:8787/` เลือก FSR หรือ JOYSTICK ในเมนู แล้วทดสอบการเลื่อนซ้าย/ขวา
5. ถ้าต้องตรวจค่าเซนเซอร์ เปิด Serial Monitor ที่ **9600 baud** ส่ง `r` หนึ่งครั้ง จะเห็น `fsr_left_raw,fsr_right_raw,joystick_y_raw` ทุก 50 ms ส่ง `r` อีกครั้งเพื่อหยุดแสดง ค่า Y ใกล้กลางประมาณ 1970; เกณฑ์หยุดอยู่ที่ 1720–2220 ค่า Y สูงตั้งต้นให้ไปซ้าย ค่าต่ำให้ไปขวา ถ้าจอยติดตั้งกลับด้าน ให้เปลี่ยน `JoystickYHighMeansLeft` เป็น `false` ใน `CatchController.ino` แล้วอัปโหลดใหม่
6. LCD 16×2 แสดงคะแนน เวลาคงเหลือ ระดับ และโหมด ถ้าไม่พบจอ เกมและเซนเซอร์ยังทำงานได้ ให้ตรวจสาย/แรงดัน/ที่อยู่ I2C และตัวต้านทานปรับความเข้มตัวอักษรหลังจอ

ถ้าเปลี่ยนเครือข่ายหรือ IPv4 คอม ให้แก้ `BrokerHost` ใน `Settings.h` และ `CATCHLAB_MQTT_HOST` ใน `.env.local` ให้ตรงกัน จากนั้นอัปโหลดเฟิร์มแวร์ใหม่และเปิด Bridge ใหม่

## เปิดซอร์สใน Unity และสร้าง Web Build ใหม่

1. ใน Unity Hub เพิ่มโปรเจกต์จากโฟลเดอร์ `CatchLabFriend` และเปิดด้วย Unity 6000.5.2f1
2. เปิด `Assets/Scenes/SampleScene.unity` แล้วกด Play เพื่อทดสอบใน Editor
3. ใช้เมนู **CatchLab Friend > Build Web** เพื่อสร้างเว็บ ผลลัพธ์จะอยู่ที่ `CatchLabFriend/Build/Web` (ZIP ขนาดเล็กไม่ได้แนบ Build สำเร็จรูป)
4. ถ้า Bridge เปิดอยู่ ให้หยุดด้วย `Ctrl+C` แล้วเริ่ม `npm.cmd run friend` ใหม่ จากนั้นรีเฟรชหน้าเว็บ

ไม่ต้องคัดลอกโฟลเดอร์ `Library`, `Temp` หรือ `Logs` ไปเครื่องอื่น Unity จะสร้างใหม่เมื่อเปิดโปรเจกต์ครั้งแรก

## เปิด Firebase และ Google Sheets

1. สร้างโปรเจกต์ Firebase เปิด Cloud Firestore และสร้าง service-account JSON จาก Firebase Admin SDK เก็บไฟล์ JSON **นอก ZIP/โปรเจกต์**
2. ใน `ControllerBridge/.env.local` ใส่ `CATCHLAB_FIREBASE_PROJECT_ID` และ `GOOGLE_APPLICATION_CREDENTIALS` ชี้ไปยัง JSON นั้น
3. ถ้าต้องการ Google Sheets ให้เปิด Google Sheets API ใน Google Cloud โปรเจกต์เดียวกัน แชร์ชีตให้ `client_email` ใน JSON เป็น Editor แล้วใส่ Spreadsheet ID ใน `CATCHLAB_SHEET_ID`
4. รัน `npm.cmd run sheets-sync` หนึ่งครั้งถ้ามีรอบเก่า หลังจากนั้นรอบใหม่จะซิงก์อัตโนมัติเมื่อ Bridge ทำงาน
5. เปิด Firebase Console > Firestore > `players/{playerId}/rounds/{roundId}` เพื่อตรวจข้อมูล หรือดูแท็บ `CatchLab Rounds` และ `Dashboard` ในชีต

ข้อมูลรอบประกอบด้วยชื่อผู้เล่น คะแนน ระดับ เวลา จำนวนของที่รับได้/ทั้งหมด เวลาเล่น และจำนวนครั้งที่เล่น Bridge รับข้อมูลจาก Unity ที่ `/rounds` เมื่อจบรอบ แล้วบันทึกลง Firebase ก่อนคัดลอกไป Google Sheets ถ้าชีตมีปัญหา Firebase ยังคงบันทึกผลได้

## ปัญหาที่พบบ่อย

| อาการ | ตรวจ |
| --- | --- |
| `EADDRINUSE` ที่ 1883 หรือ 8787 | มี Bridge เปิดอยู่แล้ว ใช้หน้าต่างเดิมหรือหยุดด้วย `Ctrl+C` ก่อนเริ่มใหม่ |
| เกมเปิดได้ แต่เซนเซอร์ไม่ขยับ | เลือกโหมดให้ตรง, ตรวจ MQTT/IPv4, ตรวจ Serial Monitor และ `/controller/latest` |
| จอยขยับกลับด้าน | เปลี่ยน `JoystickYHighMeansLeft` เป็น `false` แล้วอัปโหลดใหม่ |
| ขึ้น `MQTT connection failed (-2)` | ตรวจคอมและ ESP อยู่เครือข่ายเดียวกัน, `BrokerHost`, พอร์ต 1883 และไฟร์วอลล์ |
| จอมีไฟแต่ไม่มีข้อความ | ตรวจ SDA/SCL, แรงดัน/ตัวแปลงระดับ และปรับคอนทราสต์ที่หลังจอ |
| ผลไม่ขึ้น Firebase/Sheets | ตรวจ `.env.local`, service account, สิทธิ์ Firestore/Sheets API และข้อความในหน้าต่าง Bridge |

## หลักฐานสาธิตประกอบงาน

โปสเตอร์และคลิปไม่ได้อยู่ในชุด code.zip นี้ ผู้ส่งงานต้องจัดทำเองตามใบเกณฑ์: โปสเตอร์ A1 และคลิปแนวตั้ง 9:16 ความละเอียด FHD 60 FPS ความยาวไม่เกิน 3 นาที ควรแสดงเลือก 3 ระดับ, เปลี่ยนฉาก, ของ 3 แบบ, Combo, สลับ FSR/จอย, ค่า ESP32/MQTT, LCD และข้อมูลรอบที่บันทึกจริงใน Firebase หรือ Google Sheets
