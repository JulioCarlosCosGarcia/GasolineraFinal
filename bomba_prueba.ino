#include <ArduinoJson.h>

volatile double flujo; // Variable para el flujo de agua
int pinBomba = 3; // Pin de la bomba
double limite = 0; // Inicializamos el límite en litros
bool despachoEnProgreso = false; // Indica si el despacho está en progreso
String bombaSeleccionada; // Bomba seleccionada
bool tanqueLlenoActivado = false; // Indica si el modo tanque lleno está activado

void setup() {
  Serial.begin(9600);
  flujo = 0;
  attachInterrupt(digitalPinToInterrupt(2), pulse, RISING); // Configurar interrupción para medir flujo
  pinMode(pinBomba, OUTPUT); // Configurar pin de la bomba como salida
  pinMode(4, INPUT); // Configurar pin del dispensador como entrada
  pinMode(5, INPUT); // Configurar pin del sensor de tanque lleno como entrada
}

void loop() {
  // Comprobar si hay datos recibidos desde el puerto serial
  while (Serial.available() > 0) {
    String datosRecibidos = Serial.readStringUntil('\n');
    StaticJsonDocument<200> doc;
    DeserializationError error = deserializeJson(doc, datosRecibidos);

    // Si hay un error en la deserialización, continuar
    if (error) {
      Serial.println("Error al leer JSON");
      continue;
    }

    // Leer la acción del JSON recibido
    const char* accion = doc["action"];
    double valor = doc["value"];
    const char* bomba = doc["bomba"];

    // Realizar la acción correspondiente
    if (strcmp(accion, "calibracion") == 0) {
      limite = valor * 9; // Establecer el límite en litros
      flujo = 0; // Reiniciar el flujo a cero
      despachoEnProgreso = false; // Resetear el estado del despacho
      bombaSeleccionada = String(bomba); // Almacenar la bomba seleccionada
      tanqueLlenoActivado = false; // Desactivar el modo tanque lleno
    } else if (strcmp(accion, "tanque_lleno") == 0) {
      flujo = 0; // Reiniciar el flujo a cero
      despachoEnProgreso = true; // Iniciar el estado del despacho
      bombaSeleccionada = String(bomba); // Almacenar la bomba seleccionada
      tanqueLlenoActivado = true; // Activar el modo tanque lleno
      limite = -1; // Indicar que no hay límite
    } else if (strcmp(accion, "flujo") == 0) {
      flujo = valor; // Reiniciar el flujo a cero
    }
  }

  // Controlar la bomba según el modo de operación
  if (tanqueLlenoActivado) {
    controlarTanqueLleno();
  } else {
    controlarPrepago();
  }

  // Enviar el valor de flujo y si el límite ha sido alcanzado como JSON
  enviarEstado();
  
  delay(100); // Reducir el delay para mayor fluidez
}

void pulse() {
  flujo += 1.0 / 860.0; // Calibración recomendada para medir el flujo
}

void controlarTanqueLleno() {
  int dispensador = digitalRead(4);
  int tanqueLleno = digitalRead(5);
  
  if (dispensador == 1 && tanqueLleno == 0) {
    despachoEnProgreso = true;
    digitalWrite(pinBomba, HIGH);
  } else {
    digitalWrite(pinBomba, LOW);
    if (tanqueLleno == HIGH) {
      despachoEnProgreso = false;
      tanqueLlenoActivado = false; // Desactivar el modo tanque lleno después de completar
    }
  }
}

void controlarPrepago() {
  int dispensador = digitalRead(4);
  
  if (dispensador == 1 && flujo < limite) {
    despachoEnProgreso = true;
    digitalWrite(pinBomba, HIGH);
  } else {
    digitalWrite(pinBomba, LOW);
    if (flujo >= limite && despachoEnProgreso) {
      despachoEnProgreso = false;
    }
  }
}

void enviarEstado() {
  StaticJsonDocument<200> doc;
  doc["flujo"] = flujo / 9; // Mantener en litros para mayor precisión
  doc["limiteAlcanzado"] = !despachoEnProgreso; // Indicar si el despacho ha terminado
  doc["bomba"] = bombaSeleccionada.c_str(); // Enviar la bomba correcta
  String salida;
  serializeJson(doc, salida);
  Serial.println(salida);
}
