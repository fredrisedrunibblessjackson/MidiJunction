﻿using System;
using System.Collections.Generic;
using System.Linq;
using MidiJunction.Devices;

namespace MidiJunction.Classes
{
    public static class NoteManager
    {
        public class Chord
        {
            public string Name;
            public int Preference;
            public int[] Sequence;

            public Chord(int preference, string name, params int[] sequence)
            {
                Name = name;
                Preference = preference;
                Sequence = sequence;
            }
        }

        private static readonly List<Chord> Chords = new List<Chord>
        {
            // Basic chords
            new Chord(1, "",       0, 4, 7),
            new Chord(1, "m",      0, 3, 7),

            // Seconds and fourths
            new Chord(2, "2",      0, 2),
            new Chord(2, "2",      0, 2, 7),
            new Chord(2, "sus4",   0, 5),
            new Chord(2, "sus4",   0, 5, 7),
            new Chord(2, "2sus4",  0, 2, 5),

            // Fifths
            new Chord(3, "5",      0, 7),

            // Sevenths
            new Chord(4, "7",      0, 4, 7, 10),
            new Chord(4, "maj7",   0, 4, 7, 11),
            new Chord(4, "m7",     0, 3, 7, 10),
            new Chord(4, "mmaj7",  0, 3, 7, 11),

            // Diminished
            new Chord(5, "dim",    0, 3, 6),
            new Chord(5, "dim",    0, 3, 6, 9),

            // Sixths
            new Chord(6, "6",      0, 4, 9),
            new Chord(6, "6",      0, 4, 7, 9),
            new Chord(6, "m6",     0, 3, 9),
            new Chord(6, "m6",     0, 3, 7, 9),

            // Just thirds
            new Chord(7, "",       0, 4),
            new Chord(7, "m",      0, 3),

            // Sixths without thirds
            new Chord(8, "6no3",   0, 9),
            new Chord(8, "6no3",   0, 7, 9),
        };

        private static readonly SortedSet<int> Active = new SortedSet<int>();

        private static string _lastChord;
        public static event MidiMessageHandler MidiMessage;
        public static int CurrentChannel { get; set; }

        public static bool IsNoteOn(byte note)
        {
            return Active.Contains(note);
        }

        public static int NoteOnCount()
        {
            return Active.Count;
        }

        public static int[] NotesOn()
        {
            return Active.ToArray();
        }

        public static string MidiNoteToDisplayNoteAndOctave(int note)
        {
            return MidiNoteToDisplayNote(note) + (note / 12 - 2);
        }

        public static string MidiNoteToDisplayNote(int note)
        {
            switch (note % 12)
            {
                case 0: return "C";
                case 1: return "C#";
                case 2: return "D";
                case 3: return "D#";
                case 4: return "E";
                case 5: return "F";
                case 6: return "F#";
                case 7: return "G";
                case 8: return "G#";
                case 9: return "A";
                case 10: return "A#";
                case 11: return "B";
                default: return null;
            }
        }

        public static string MidiNotesToChord(int[] notes)
        {
            if (notes.Length < 2)
                return null;

            // offset, preference, name
            var chords = new List<Tuple<int, int, string>>();

            var times = 0;
            foreach (var offset in notes)
            {
                var balancedNotes = notes
                    .Select(x => (x - offset + 12) % 12)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToArray();

                var chord = MatchChord(balancedNotes);
                if (chord != null)
                    chords.Add(new Tuple<int, int, string>(offset, chord.Preference + times, chord.Name));

                times++;
            }

            if (!chords.Any())
                return "";

            var found = chords.OrderBy(x => x.Item2).First();
            return MidiNoteToDisplayNote(found.Item1) + found.Item3;
        }

        private static Chord MatchChord(int[] notes)
        {
            return Chords.FirstOrDefault(c => MatchChord(c.Sequence, notes));
        }

        private static bool MatchChord(int[] sequence, int[] notes)
        {
            if (sequence.Length != notes.Length)
                return false;

            for (var i=0; i<sequence.Length; i++)
                if (sequence[i] != notes[i])
                    return false;

            return true;
        }

        public static void Activate(int note)
        {
            _lastChord = null;
            Active.Add(note);
        }

        public static void Deactivate(int note)
        {
            Active.Remove(note);
            if (!Active.Any())
                _lastChord = null;
        }

        public static void TurnNoteOn(int note, int velocity)
        {
            MidiMessage?.Invoke(null, new MidiMessageEventArgs(0, Devices.MidiMessage.NoteOn(CurrentChannel, note, velocity)));
        }

        public static void TurnNoteOff(int note)
        {
            MidiMessage?.Invoke(null, new MidiMessageEventArgs(0, Devices.MidiMessage.NoteOff(CurrentChannel, note, 0)));
        }

        public static void TurnAllNotesOff()
        {
            foreach(var note in NotesOn())
                TurnNoteOff(note);
        }

        public static string CurrentChord()
        {
            return _lastChord ?? (_lastChord = MidiNotesToChord(NotesOn()));
        }
    }
}