#include <OneWire.h> // libreria para el sensor de temperatura
#include <DallasTemperature.h>
#include <Wire.h>
#include <VL53L0X.h>

#define ONE_WIRE_BUS 3


#define valv_in   4 // valvula de entrada
#define valv_out  5// valvula de salida
#define motor     6 // bomba
#define sensor_caudal 2 // sensor caudal

//#define HIGH_SPEED
#define HIGH_ACCURACY
//#define LONG_RANGE

VL53L0X Sen_Dist;
OneWire oneWire(ONE_WIRE_BUS);
DallasTemperature Sen_Temp(&oneWire);

int count = 0;
float temp;
int dist;
String texto, text[5];
int index = 0;
int caudal = 0;
int presion = 0;
char *token = NULL;

int litros_Hora; // Variable que almacena el caudal (L/hora)
volatile int pulsos = 0; // Variable que almacena el número de pulsos
unsigned long tiempoAnterior = 0; // Variable para calcular el tiempo transcurrido
unsigned long pulsos_Acumulados = 0; // Variable que almacena el número de pulsos acumulados
float litros; // // Variable que almacena el número de litros acumulados

int sensorValue = 0;

void flujo()
{
  pulsos++; // Incrementa en una unidad el número de pulsos
}

void setup() {

  pinMode(valv_in, OUTPUT);
  pinMode(valv_out, OUTPUT);
  pinMode(motor, OUTPUT);


  digitalWrite(valv_in, LOW);
  digitalWrite(valv_out, LOW);
  digitalWrite(motor, LOW);

  Serial.begin(9600);
  Serial.setTimeout(100);
  Sen_Temp.begin();

  Wire.begin();

  Sen_Dist.init();
  Sen_Dist.setTimeout(500);
  //***********************************************

  interrupts(); // Habilito las interrupciones
  //  Interrupción INT0, llama a la ISR llamada "flujo" en cada flanco de subida en el pin digital 2
  attachInterrupt(digitalPinToInterrupt(sensor_caudal), flujo, RISING);

  //***********************************************

  //***********************************************


#if defined LONG_RANGE
  // lower the return signal rate limit (default is 0.25 MCPS)
  Sen_Dist.setSignalRateLimit(0.1);
  // increase laser pulse periods (defaults are 14 and 10 PCLKs)
  Sen_Dist.setVcselPulsePeriod(VL53L0X::VcselPeriodPreRange, 18);
  Sen_Dist.setVcselPulsePeriod(VL53L0X::VcselPeriodFinalRange, 14);
#endif

#if defined HIGH_SPEED
  // reduce timing budget to 20 ms (default is about 33 ms)
  Sen_Dist.setMeasurementTimingBudget(20000);
#elif defined HIGH_ACCURACY
  // increase timing budget to 200 ms
  Sen_Dist.setMeasurementTimingBudget(200000);
#endif
  //********************************************

}
/*protocolo de respuesta del sistema SCADA
   nivel del tanque, temperatura del interior del tanque, presion descaraga de la bomba, caudal, NL & CR
   nivel del tanque = 0 a 50
   temperatura =      0 a 120
   presion =          0 a 100
   caudal =           0 a 30

*/
void loop() {

  tiempoAnterior = millis(); // Guardo el tiempo que tarda el ejecutarse el setup
  while (true) {

    calcular_flujo();

    if (Serial.available())
    {
      texto = Serial.readStringUntil('\n');

      fragmentar();
      analizar_texto();

      calcular_flujo();
      leer_sensores();

      calcular_flujo();

      Serial.print(dist);
      Serial.print(",");
      Serial.print(temp, 1);
      Serial.print(",");
      Serial.print(presion);
      Serial.print(",");
      Serial.println(caudal);

    }
  }
}

void calcular_flujo() {
  // Cada segundo calculamos e imprimimos el caudal y el número de litros consumidos
  if (millis() - tiempoAnterior > 1000)
  {
    // Realizo los cálculos
    tiempoAnterior = millis(); // Actualizo el nuevo tiempo
    pulsos_Acumulados += pulsos; // Número de pulsos acumulados
    litros_Hora = (pulsos / 7.5); // Q = frecuencia * 60/ 7.5 (L/Hora)
    litros = pulsos_Acumulados * 1.0 / 450; // Cada 450 pulsos son un litro
    pulsos = 0; // Pongo nuevamente el número de pulsos a cero
    caudal = litros_Hora;
  }
}


void leer_sensores() {
  temp = 0;
  dist = 0;
  presion = 0;
  Sen_Temp.requestTemperatures();
  dist = Sen_Dist.readRangeSingleMillimeters() / 10;
  temp = Sen_Temp.getTempCByIndex(0);
  presion = analogRead(A1);

}
void fragmentar() {
  index = 0;
  token = strtok(texto.c_str(), "," );

  while ( token != NULL )
  {
    text[index] = String(token);
    index++;
    token = strtok( NULL, "," );
  }
}
void analizar_texto() {
  if (text[0].equals("#on")) {
    digitalWrite(valv_in, HIGH);
  }
  else {
    digitalWrite(valv_in, LOW);
  }
  if (text[1].equals("#on")) {
    digitalWrite(valv_out, HIGH);
  }
  else {
    digitalWrite(valv_out, LOW);
  }
  if (text[2].equals("#on")) {
    digitalWrite(motor, HIGH);
  }
  else {
    digitalWrite(motor, LOW);
  }
}
/*
  while (true) {
    if (Serial.available())
    {
      if (text2.equals(Serial.readStringUntil('\n')) ){
      Serial.println(text1 + count);
        count++;
        delay(700);
      }
    }
  }
*/
