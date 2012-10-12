﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

#if VS_UNIT_TESTING
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using NUnit.Framework;
using TestClassAttribute = NUnit.Framework.TestFixtureAttribute;
using TestMethodAttribute = NUnit.Framework.TestAttribute;
#endif

namespace CLanguage.Tests
{
    [TestClass]
    public class ArduinoTests
    {
        TranslationUnit Parse(string code)
        {
            var pp = new Preprocessor();
            pp.AddCode("stdin", code);
            var lexer = new Lexer(pp);
            var parser = new CParser();
			var report = new Report (new TextWriterReportPrinter (Console.Out));
            return parser.ParseTranslationUnit(lexer, report);
        }

		public const string BlinkCode = @"
int OUTPUT = 0;
int HIGH = 1;
int LOW = 0;
void setup() {                
  // initialize the digital pin as an output.
  // Pin 13 has an LED connected on most Arduino boards:
  pinMode(13, OUTPUT);     
}

void loop() {
  digitalWrite(13, HIGH);   // set the LED on
  delay(1000);              // wait for a second
  digitalWrite(13, LOW);    // set the LED off
  delay(1000);              // wait for a second
}
";

        [TestMethod]
        public void Blink()
        {
            var tu = Parse(BlinkCode);

            Assert.AreEqual(2, tu.Functions.Count);

            var setup = tu.Functions[0];
            Assert.AreEqual("setup", setup.Name);
            Assert.AreEqual(1, setup.Body.Statements.Count);

            var loop = tu.Functions[1];
            Assert.AreEqual("loop", loop.Name);
            Assert.AreEqual(4, loop.Body.Statements.Count);
        }

        [TestMethod]
        public void DigitalReadSerial()
        {
            var code = @"
void setup() {
  Serial.begin(9600);
  pinMode(2, INPUT);
}

void loop() {
  int sensorValue = digitalRead(2);
  Serial.println(sensorValue, DEC);
}
";
            var tu = Parse(code);

            Assert.AreEqual(2, tu.Functions.Count);

            var setup = tu.Functions[0];
            Assert.AreEqual("setup", setup.Name);
            Assert.AreEqual(2, setup.Body.Statements.Count);

            var loop = tu.Functions[1];
            Assert.AreEqual("loop", loop.Name);
            Assert.AreEqual(1, loop.Body.Variables.Count);
            Assert.AreEqual(2, loop.Body.Statements.Count);

            Assert.IsInstanceOf<ExpressionStatement>(loop.Body.Statements[0]);
            Assert.IsInstanceOf<AssignExpression>(((ExpressionStatement)loop.Body.Statements[0]).Expression);
            Assert.IsInstanceOf<ExpressionStatement>(loop.Body.Statements[1]);
            Assert.IsInstanceOf<FuncallExpression>(((ExpressionStatement)loop.Body.Statements[1]).Expression);

            var println = (FuncallExpression)((ExpressionStatement)loop.Body.Statements[1]).Expression;

            Assert.AreEqual(2, println.Arguments.Count);

			Assert.IsInstanceOf<MemberFromReferenceExpression>(println.Function);
        }

		public const string FadeCode = @"
int OUTPUT = 0;

int brightness = 0;    // how bright the LED is
int fadeAmount = 5;    // how many points to fade the LED by

void setup()  { 
  // declare pin 9 to be an output:
  pinMode(9, OUTPUT);
} 

void loop()  { 
  // set the brightness of pin 9:
  analogWrite(9, brightness);    

  // change the brightness for next time through the loop:
  brightness = brightness + fadeAmount;

  // reverse the direction of the fading at the ends of the fade: 
  if (brightness == 0 || brightness == 255) {
    fadeAmount = -fadeAmount ; 
  }     
  // wait for 30 milliseconds to see the dimming effect    
  delay(30);                            
}
";

        [TestMethod]
        public void Fade()
        {            
            var tu = Parse(FadeCode);

            Assert.AreEqual(2, tu.Functions.Count);
            Assert.AreEqual(3, tu.Variables.Count);

            var setup = tu.Functions[0];
            Assert.AreEqual("setup", setup.Name);
            Assert.AreEqual(1, setup.Body.Statements.Count);

            var loop = tu.Functions[1];
            Assert.AreEqual("loop", loop.Name);
            Assert.AreEqual(0, loop.Body.Variables.Count);
            Assert.AreEqual(4, loop.Body.Statements.Count);

            Assert.IsInstanceOf<ExpressionStatement>(loop.Body.Statements[1]);
            Assert.IsInstanceOf<AssignExpression>(((ExpressionStatement)loop.Body.Statements[1]).Expression);
            
			Assert.IsInstanceOf<IfStatement>(loop.Body.Statements[2]);
            var iff = (IfStatement)loop.Body.Statements[2];
            Assert.IsInstanceOf<BinaryExpression>(iff.Condition);
            Assert.IsInstanceOf<Block>(iff.TrueStatement);
            Assert.IsNull(iff.FalseStatement);

            var tr = (Block)iff.TrueStatement;
            Assert.AreEqual(1, tr.Statements.Count);
            Assert.IsInstanceOf<ExpressionStatement>(tr.Statements[0]);
            Assert.IsInstanceOf<AssignExpression>(((ExpressionStatement)tr.Statements[0]).Expression);

            var r = ((AssignExpression)((ExpressionStatement)tr.Statements[0]).Expression).Right;
            Assert.IsInstanceOf<UnaryExpression>(r);
            Assert.AreEqual(Unop.Negate, ((UnaryExpression)r).Op);
        }

        [TestMethod]
        public void Tone()
        {
            var code = @"
int melody[] = {
  NOTE_C4, NOTE_G3,NOTE_G3, NOTE_A3, NOTE_G3,0, NOTE_B3, NOTE_C4};

// note durations: 4 = quarter note, 8 = eighth note, etc.:
int noteDurations[] = {
  4, 8, 8, 4,4,4,4,4 };

void setup() {
  // iterate over the notes of the melody:
  for (int thisNote = 0; thisNote < 8; thisNote++) {

    // to calculate the note duration, take one second 
    // divided by the note type.
    //e.g. quarter note = 1000 / 4, eighth note = 1000/8, etc.
    int noteDuration = 1000/noteDurations[thisNote];
    tone(8, melody[thisNote],noteDuration);

    // to distinguish the notes, set a minimum time between them.
    // the note's duration + 30% seems to work well:
    int pauseBetweenNotes = noteDuration * 1.30;
    delay(pauseBetweenNotes);
    // stop the tone playing:
    noTone(8);
  }
}

void loop() {
  // no need to repeat the melody.
}
";
            var tu = Parse(code);

            Assert.AreEqual(2, tu.Functions.Count);
            Assert.AreEqual(2, tu.Variables.Count);
            Assert.AreEqual(2, tu.Statements.Count);

            var len = ((CArrayType)tu.Variables[0].VariableType).LengthExpression;
            Assert.IsInstanceOf<ConstantExpression>(len);
            Assert.AreEqual(8, ((ConstantExpression)len).Value);
            
            Assert.IsInstanceOf<StructureExpression>(((AssignExpression)((ExpressionStatement)tu.Statements[0]).Expression).Right);
            var st = (StructureExpression)((AssignExpression)((ExpressionStatement)tu.Statements[0]).Expression).Right;
            Assert.AreEqual(8, st.Items.Count);

            var setup = tu.Functions[0];
            Assert.AreEqual("setup", setup.Name);
            Assert.AreEqual(1, setup.Body.Statements.Count);

            var f = (ForStatement)setup.Body.Statements[0];

            Assert.AreEqual(1, f.InitBlock.Variables.Count);
            Assert.AreEqual(1, f.InitBlock.Statements.Count);
            Assert.IsInstanceOf<BinaryExpression>(f.ContinueExpression);
            Assert.AreEqual(Binop.LessThan, ((BinaryExpression)f.ContinueExpression).Op);
            Assert.IsInstanceOf<UnaryExpression>(f.NextExpression);
            Assert.AreEqual(Unop.PostIncrement, ((UnaryExpression)f.NextExpression).Op);
            Assert.AreEqual("thisNote", ((VariableExpression)((UnaryExpression)f.NextExpression).Right).VariableName);

            Assert.IsInstanceOf<Block>(f.LoopBody);
            var b = (Block)f.LoopBody;
            Assert.AreEqual(2, b.Variables.Count);
            Assert.AreEqual(5, b.Statements.Count);

            var tone = (FuncallExpression)((ExpressionStatement)b.Statements[1]).Expression;
            Assert.AreEqual(3, tone.Arguments.Count);
			Assert.IsInstanceOf<ArrayElementExpression>(tone.Arguments[1]);

            var loop = tu.Functions[1];
            Assert.AreEqual("loop", loop.Name);
            Assert.AreEqual(0, loop.Body.Variables.Count);
            Assert.AreEqual(0, loop.Body.Statements.Count);
        }

        [TestMethod]
        public void Calibration()
        {
            var code = @"
// These constants won't change:
const int sensorPin = A0;    // pin that the sensor is attached to
const int ledPin = 9;        // pin that the LED is attached to

// variables:
int sensorValue = 0;         // the sensor value
int sensorMin = 1023;        // minimum sensor value
int sensorMax = 0;           // maximum sensor value


void setup() {
  // turn on LED to signal the start of the calibration period:
  pinMode(13, OUTPUT);
  digitalWrite(13, HIGH);

  // calibrate during the first five seconds 
  while (millis() < 5000) {
    sensorValue = analogRead(sensorPin);

    // record the maximum sensor value
    if (sensorValue > sensorMax) {
      sensorMax = sensorValue;
    }

    // record the minimum sensor value
    if (sensorValue < sensorMin) {
      sensorMin = sensorValue;
    }
  }

  // signal the end of the calibration period
  digitalWrite(13, LOW);
}

void loop() {
  // read the sensor:
  sensorValue = analogRead(sensorPin);

  // apply the calibration to the sensor reading
  sensorValue = map(sensorValue, sensorMin, sensorMax, 0, 255);

  // in case the sensor value is outside the range seen during calibration
  sensorValue = constrain(sensorValue, 0, 255);

  // fade the LED using the calibrated value:
  analogWrite(ledPin, sensorValue);
}
";
            var tu = Parse(code);

            Assert.AreEqual(2, tu.Functions.Count);
            Assert.AreEqual(5, tu.Variables.Count);
            Assert.AreEqual(5, tu.Statements.Count);

            Assert.AreEqual(TypeQualifiers.Const, tu.Variables[0].VariableType.TypeQualifiers);
            Assert.AreEqual(TypeQualifiers.Const, tu.Variables[1].VariableType.TypeQualifiers);
            Assert.AreEqual(TypeQualifiers.None, tu.Variables[2].VariableType.TypeQualifiers);
            Assert.AreEqual(TypeQualifiers.None, tu.Variables[3].VariableType.TypeQualifiers);

            var setup = tu.Functions[0];
            Assert.AreEqual("setup", setup.Name);
            Assert.AreEqual(4, setup.Body.Statements.Count);

            var loop = tu.Functions[1];
            Assert.AreEqual("loop", loop.Name);
            Assert.AreEqual(0, loop.Body.Variables.Count);
            Assert.AreEqual(4, loop.Body.Statements.Count);
        }
    }
}
