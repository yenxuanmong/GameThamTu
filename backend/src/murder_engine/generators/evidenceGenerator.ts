// ============================================
// Real Evidence Generator
// ============================================

import type { Evidence } from '../models/Evidence';
import type { MurderWeapon, MurderLocation } from '../../types/case.types';
import type { Suspect } from '../models/Suspect';
import type { TimelineEvent } from '../models/Timeline';
import type { SeededRandom } from '../../utils/random';
import { createEvidence } from '../models/Evidence';
import type { EvidenceType, EvidenceLocation } from '../../types/evidence.types';

// ---- Evidence templates by weapon ----

interface EvidenceTemplate {
  name: string;
  description: string;
  type: EvidenceType;
  location: EvidenceLocation;
  isKeyEvidence: boolean; // directly points to killer
}

const WEAPON_EVIDENCE: Record<MurderWeapon, EvidenceTemplate[]> = {
  knife: [
    { name: 'Blood-stained knife', description: 'A carving knife with traces of blood matching the victim. One clear fingerprint on the handle.', type: 'physical', location: 'crime_scene', isKeyEvidence: true },
    { name: 'Knife sheath', description: 'An empty leather sheath found near the body — fits a standard carving knife.', type: 'physical', location: 'crime_scene', isKeyEvidence: false },
    { name: 'Bloodstained cloth', description: 'A cloth bearing the victim\'s blood group, discarded nearby — possibly used to wipe the blade.', type: 'physical', location: 'crime_scene', isKeyEvidence: false },
    { name: 'Forensic wound analysis', description: 'Lab report confirms wound consistent with a single-edged blade, 15cm long. Struck from behind and slightly above.', type: 'forensic', location: 'forensic_lab', isKeyEvidence: false },
  ],
  gun: [
    { name: 'Spent cartridge', description: 'A single brass cartridge casing found at the scene — calibre .32. Partial fingerprint visible.', type: 'physical', location: 'crime_scene', isKeyEvidence: true },
    { name: 'Gunshot residue report', description: 'Forensic swabs taken from suspects — one tested positive for GSR.', type: 'forensic', location: 'forensic_lab', isKeyEvidence: true },
    { name: 'Firearm purchase record', description: 'Registry shows a .32 calibre revolver registered to a name in the household.', type: 'document', location: 'forensic_lab', isKeyEvidence: false },
    { name: 'Bullet trajectory analysis', description: 'Shot fired from within 2 metres — the shooter was known to the victim.', type: 'forensic', location: 'forensic_lab', isKeyEvidence: false },
  ],
  poison: [
    { name: 'Toxicology report', description: 'Confirms lethal dose of arsenic trioxide in the victim\'s stomach contents — ingested approximately 2 hours before death.', type: 'forensic', location: 'forensic_lab', isKeyEvidence: true },
    { name: 'Partially emptied vial', description: 'A small glass vial found in the kitchen — residue tests positive for arsenic.', type: 'physical', location: 'crime_scene', isKeyEvidence: true },
    { name: 'Rat poison tin', description: 'Tin of rat poison found in the garden shed — contents partially depleted, formula matches the toxin found.', type: 'physical', location: 'suspect_residence', isKeyEvidence: false },
    { name: 'Victim\'s final drink', description: 'A half-finished glass of port on the side table — traces of arsenic confirmed in sediment.', type: 'physical', location: 'crime_scene', isKeyEvidence: false },
  ],
  blunt_object: [
    { name: 'Bloodied paperweight', description: 'A heavy brass paperweight bearing traces of blood and a fragment of scalp tissue. Hair caught on the edge.', type: 'physical', location: 'crime_scene', isKeyEvidence: true },
    { name: 'Skull fracture report', description: 'Post-mortem confirms blunt force trauma — wound consistent with a rounded, heavy object.', type: 'forensic', location: 'forensic_lab', isKeyEvidence: false },
    { name: 'Broken trophy base', description: 'An award trophy with its base missing — the detached section has not been found.', type: 'physical', location: 'crime_scene', isKeyEvidence: false },
    { name: 'Transfer fibres', description: 'Fibres from an unknown fabric found on the victim\'s collar — possibly left by the attacker.', type: 'forensic', location: 'forensic_lab', isKeyEvidence: false },
  ],
  strangulation: [
    { name: 'Ligature cord', description: 'A length of braided cord with knot marks consistent with the ligature marks on the victim\'s neck.', type: 'physical', location: 'crime_scene', isKeyEvidence: true },
    { name: 'Hyoid fracture report', description: 'Post-mortem confirms fracture of the hyoid bone — consistent with manual or ligature strangulation.', type: 'forensic', location: 'forensic_lab', isKeyEvidence: false },
    { name: 'Skin under fingernails', description: 'The victim managed to scratch their attacker — DNA extracted for comparison.', type: 'forensic', location: 'forensic_lab', isKeyEvidence: true },
    { name: 'Torn cufflink', description: 'A torn cufflink found near the body — expensive, monogrammed. One of a pair.', type: 'physical', location: 'crime_scene', isKeyEvidence: false },
  ],
  drowning: [
    { name: 'Wet footprints', description: 'Footprints in the mud leading to and from the water — size and stride suggest an adult of average height.', type: 'physical', location: 'crime_scene', isKeyEvidence: false },
    { name: 'Diatom analysis', description: 'Diatoms from the victim\'s lungs match the water source on the property, not the river nearby.', type: 'forensic', location: 'forensic_lab', isKeyEvidence: true },
    { name: 'Bruising pattern', description: 'Bruising on the victim\'s shoulders consistent with being held forcibly beneath the surface.', type: 'forensic', location: 'forensic_lab', isKeyEvidence: false },
    { name: 'Broken fingernail', description: 'A broken fingernail fragment found on the bath surround — not the victim\'s.', type: 'forensic', location: 'crime_scene', isKeyEvidence: true },
  ],
  fire: [
    { name: 'Accelerant traces', description: 'Residue analysis confirms the presence of petroleum-based accelerant at the point of ignition.', type: 'forensic', location: 'crime_scene', isKeyEvidence: true },
    { name: 'Fuel canister', description: 'An empty fuel canister found in an outbuilding — traces of the same accelerant detected.', type: 'physical', location: 'suspect_residence', isKeyEvidence: true },
    { name: 'Singed note', description: 'A partially burned note recovered from the scene — some words are legible and suggest premeditation.', type: 'document', location: 'crime_scene', isKeyEvidence: false },
    { name: 'Witness to smoke', description: 'A neighbour spotted someone leaving the property shortly before the fire was noticed.', type: 'circumstantial', location: 'public_area', isKeyEvidence: false },
  ],
  explosion: [
    { name: 'Detonator fragment', description: 'A fragment of a commercial detonator recovered from the blast site — traceable to a specific supplier.', type: 'physical', location: 'crime_scene', isKeyEvidence: true },
    { name: 'Purchase receipt', description: 'A receipt for blasting materials at a hardware merchant — signed or initialled by the purchaser.', type: 'document', location: 'suspect_residence', isKeyEvidence: true },
    { name: 'Blast pattern analysis', description: 'Expert report confirms device was placed deliberately beneath the victim\'s chair.', type: 'forensic', location: 'forensic_lab', isKeyEvidence: false },
    { name: 'Chemical residue', description: 'Residue consistent with the explosive compound found on clothing belonging to one of the suspects.', type: 'forensic', location: 'forensic_lab', isKeyEvidence: true },
  ],
  fall: [
    { name: 'Scuffed railing paint', description: 'Fresh scuff marks on the balcony railing at waist height — consistent with someone being pushed against it.', type: 'physical', location: 'crime_scene', isKeyEvidence: false },
    { name: 'Trajectory analysis', description: 'The victim\'s landing position is inconsistent with a simple trip or stumble — projection suggests external force.', type: 'forensic', location: 'forensic_lab', isKeyEvidence: true },
    { name: 'Fibres on railing', description: 'Fibres from an unidentified garment caught on the railing — colour and weave are noted.', type: 'forensic', location: 'crime_scene', isKeyEvidence: false },
    { name: 'Witness account', description: 'A passerby below claims to have seen two figures on the balcony moments before the fall.', type: 'testimonial', location: 'public_area', isKeyEvidence: false },
  ],
  other: [
    { name: 'Unidentified substance', description: 'An unidentified compound found on the body — analysis is pending.', type: 'forensic', location: 'forensic_lab', isKeyEvidence: false },
    { name: 'Cryptic note', description: 'A note found in the victim\'s pocket — written in a hand not their own.', type: 'document', location: 'crime_scene', isKeyEvidence: false },
    { name: 'Suspicious footprint', description: 'A boot print near the scene — unusual sole pattern, size 9.', type: 'physical', location: 'crime_scene', isKeyEvidence: false },
    { name: 'Witness account', description: 'A household member heard raised voices shortly before the time of death.', type: 'testimonial', location: 'crime_scene', isKeyEvidence: false },
  ],
};

// ---- Generic supporting evidence ----

const GENERIC_EVIDENCE_TEMPLATES: EvidenceTemplate[] = [
  { name: 'Torn letter', description: 'A letter torn in two and partly burned — fragments reveal a heated dispute over money.', type: 'document', location: 'crime_scene', isKeyEvidence: false },
  { name: 'Appointment diary', description: 'The victim\'s diary, open to the day of their death — one appointment is heavily crossed out.', type: 'document', location: 'crime_scene', isKeyEvidence: false },
  { name: 'Security camera footage', description: 'Partial footage from the hallway camera — the timestamp shows someone entering at 10:47 PM.', type: 'digital', location: 'crime_scene', isKeyEvidence: false },
  { name: 'Pocket watch', description: 'The victim\'s pocket watch, smashed at 11:14 PM — the hands stopped at the moment of impact.', type: 'physical', location: 'crime_scene', isKeyEvidence: false },
  { name: 'Overheard argument', description: 'A servant reports overhearing a violent argument between the victim and an unknown party earlier that evening.', type: 'testimonial', location: 'crime_scene', isKeyEvidence: false },
  { name: 'Unsigned note', description: 'An unsigned note found in the victim\'s jacket pocket: "We need to talk tonight. Come alone."', type: 'document', location: 'crime_scene', isKeyEvidence: false },
  { name: 'Muddy boot prints', description: 'A trail of muddy boot prints leading from the rear entrance — unusual for dry weather.', type: 'physical', location: 'crime_scene', isKeyEvidence: false },
  { name: 'Will and testament', description: 'The victim\'s recently amended will — one beneficiary has been removed; another added.', type: 'document', location: 'victim_residence', isKeyEvidence: false },
  { name: 'Financial ledger', description: 'A private ledger reveals substantial sums transferred to an unnamed account over the past six months.', type: 'document', location: 'victim_residence', isKeyEvidence: false },
  { name: 'Locked box', description: 'A locked box found in the victim\'s desk — the key is missing and it has been forced open.', type: 'physical', location: 'crime_scene', isKeyEvidence: false },
  { name: 'Cigarette stub', description: 'A cigarette stub near the body — the victim did not smoke. A particular brand.', type: 'physical', location: 'crime_scene', isKeyEvidence: false },
  { name: 'Telephone exchange log', description: 'Records from the telephone exchange show a 12-minute call to the victim\'s number at 10:30 PM — origin traced to a public box nearby.', type: 'digital', location: 'digital', isKeyEvidence: false },
];

export interface EvidenceGenerationInput {
  caseId: string;
  killer: Suspect;
  suspects: Suspect[];
  victimId: string;
  weapon: MurderWeapon;
  murderLocation: MurderLocation;
  timeline: TimelineEvent[];
  numRealEvidence: number;
  rng: SeededRandom;
}

export function generateRealEvidence(input: EvidenceGenerationInput): Evidence[] {
  const { caseId, killer, suspects, victimId, weapon, numRealEvidence, rng } = input;

  const weaponTemplates = WEAPON_EVIDENCE[weapon] ?? WEAPON_EVIDENCE['other']!;
  const allTemplates = [...weaponTemplates, ...GENERIC_EVIDENCE_TEMPLATES];

  // Always include key evidence first
  const keyTemplates = weaponTemplates.filter((t) => t.isKeyEvidence);
  const nonKeyTemplates = rng.shuffle(allTemplates.filter((t) => !t.isKeyEvidence));

  const selectedTemplates = [
    ...keyTemplates,
    ...nonKeyTemplates.slice(0, Math.max(0, numRealEvidence - keyTemplates.length)),
  ].slice(0, numRealEvidence);

  return selectedTemplates.map((template, index) => {
    // Key evidence points to killer, supporting evidence may point to various suspects
    const pointsTo = template.isKeyEvidence
      ? killer.id
      : rng.nextBool(0.4)
        ? rng.pick([...suspects, { id: victimId }]).id
        : undefined;

    const relatesTo: string[] = template.isKeyEvidence
      ? [killer.id, victimId]
      : [victimId];

    return createEvidence(caseId, {
      type: template.type,
      name: template.name,
      description: enrichEvidenceDescription(template.description, killer, suspects, index, rng),
      location: template.location,
      relatesTo,
      isReal: true,
      isFakeEvidence: false,
      pointsTo,
    });
  });
}

function enrichEvidenceDescription(
  base: string,
  killer: Suspect,
  suspects: Suspect[],
  index: number,
  rng: SeededRandom
): string {
  // Occasionally, embed a subtle clue pointing toward the killer
  if (index === 0 && rng.nextBool(0.5)) {
    return `${base} The initial monogram appears to read "${killer.name.charAt(0)}.${killer.name.split(' ')[1]?.charAt(0) ?? '?'}"`;
  }
  // Occasionally name-drop a random suspect to create intrigue
  if (rng.nextBool(0.2) && suspects.length > 0) {
    const suspect = rng.pick(suspects.filter((s) => !s.isKiller));
    if (suspect) {
      return `${base} A witness recalls seeing ${suspect.name.split(' ')[0]} in the vicinity earlier.`;
    }
  }
  return base;
}
