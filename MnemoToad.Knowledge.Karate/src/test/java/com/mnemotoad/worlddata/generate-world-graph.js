// One-off generator for world-graph.json. Not part of the Karate suite itself (no .feature
// references this file at runtime) -- kept alongside the data it produced so the dataset can be
// regenerated/extended later (e.g. adding more cities) without hand-editing ~450 JSON nodes.
//
// Run with: node generate-world-graph.js
//
// Source data: countries-source.json, trimmed from https://github.com/mledoze/countries
// (dist/countries.json, MIT licensed) down to just {cca2, name, capital, region, subregion},
// filtered to unMember === true (194 countries -- includes Vatican City, which that dataset
// mislabels as a UN member, but its flag is real and the row is harmless as test data). Flag
// images: https://flagcdn.com (SVG, both country and US-state flags).
const crypto = require('crypto');
const fs = require('fs');
const path = require('path');

const countries = require('./countries-source.json');

// name -> { code, iso2 } for the 50 US states, sourced from flagcdn.com/en/codes.json.
const US_STATES = {
  Alaska: 'ak', Alabama: 'al', Arkansas: 'ar', Arizona: 'az', California: 'ca', Colorado: 'co',
  Connecticut: 'ct', Delaware: 'de', Florida: 'fl', Georgia: 'ga', Hawaii: 'hi', Iowa: 'ia',
  Idaho: 'id', Illinois: 'il', Indiana: 'in', Kansas: 'ks', Kentucky: 'ky', Louisiana: 'la',
  Massachusetts: 'ma', Maryland: 'md', Maine: 'me', Michigan: 'mi', Minnesota: 'mn',
  Missouri: 'mo', Mississippi: 'ms', Montana: 'mt', 'North Carolina': 'nc', 'North Dakota': 'nd',
  Nebraska: 'ne', 'New Hampshire': 'nh', 'New Jersey': 'nj', 'New Mexico': 'nm', Nevada: 'nv',
  'New York': 'ny', Ohio: 'oh', Oklahoma: 'ok', Oregon: 'or', Pennsylvania: 'pa',
  'Rhode Island': 'ri', 'South Carolina': 'sc', 'South Dakota': 'sd', Tennessee: 'tn',
  Texas: 'tx', Utah: 'ut', Virginia: 'va', Vermont: 'vt', Washington: 'wa', Wisconsin: 'wi',
  'West Virginia': 'wv', Wyoming: 'wy'
};

// A handful of major (non-capital) cities to demonstrate the City -> State -> Country hop, on
// top of the world-capitals layer below. Kept small and hand-picked rather than exhaustive.
const MAJOR_CITIES = [
  { name: 'Los Angeles', state: 'California' },
  { name: 'Houston', state: 'Texas' },
  { name: 'New York City', state: 'New York' },
  { name: 'Miami', state: 'Florida' },
  { name: 'Chicago', state: 'Illinois' }
];

const uuid = () => crypto.randomUUID();

const nodeTypes = {
  country: { id: uuid(), name: 'Country', description: 'A sovereign country (UN member state)' },
  state: { id: uuid(), name: 'State', description: 'A first-level administrative subdivision of a country' },
  city: { id: uuid(), name: 'City', description: 'A city or town' }
};

const relationshipTypes = {
  capitalOf: { id: uuid(), name: 'capitalOf', description: 'City is the capital of a Country' },
  stateInCountry: { id: uuid(), name: 'stateInCountry', description: 'State is located within a Country' },
  cityInState: { id: uuid(), name: 'cityInState', description: 'City is located within a State' }
};

const mediaAssets = [];
const knowledgeNodes = [];
const knowledgeRelations = [];

function addMediaAsset(url) {
  const asset = { id: uuid(), url };
  mediaAssets.push(asset);
  return asset;
}

const countryNodeIdByCca2 = {};

for (const c of countries) {
  const flag = addMediaAsset(`https://flagcdn.com/${c.cca2.toLowerCase()}.svg`);
  const countryNode = {
    id: uuid(),
    nodeTypeId: nodeTypes.country.id,
    canonicalName: c.name,
    attributes: { isoCode: c.cca2, region: c.region, subregion: c.subregion },
    media: { flag: { id: flag.id, alt_text: `Flag of ${c.name}` } }
  };
  knowledgeNodes.push(countryNode);
  countryNodeIdByCca2[c.cca2] = countryNode.id;

  const capitalNode = {
    id: uuid(),
    nodeTypeId: nodeTypes.city.id,
    canonicalName: c.capital,
    attributes: { isCapital: true }
  };
  knowledgeNodes.push(capitalNode);

  knowledgeRelations.push({
    id: uuid(),
    sourceNodeId: capitalNode.id,
    relationshipTypeId: relationshipTypes.capitalOf.id,
    targetNodeId: countryNode.id
  });
}

const usCountryNodeId = countryNodeIdByCca2['US'];
const stateNodeIdByName = {};

for (const [name, code] of Object.entries(US_STATES)) {
  const flag = addMediaAsset(`https://flagcdn.com/us-${code}.svg`);
  const stateNode = {
    id: uuid(),
    nodeTypeId: nodeTypes.state.id,
    canonicalName: name,
    attributes: { isoCode: `US-${code.toUpperCase()}` },
    media: { flag: { id: flag.id, alt_text: `Flag of ${name}` } }
  };
  knowledgeNodes.push(stateNode);
  stateNodeIdByName[name] = stateNode.id;

  knowledgeRelations.push({
    id: uuid(),
    sourceNodeId: stateNode.id,
    relationshipTypeId: relationshipTypes.stateInCountry.id,
    targetNodeId: usCountryNodeId
  });
}

for (const { name, state } of MAJOR_CITIES) {
  const cityNode = {
    id: uuid(),
    nodeTypeId: nodeTypes.city.id,
    canonicalName: name,
    attributes: { isCapital: false }
  };
  knowledgeNodes.push(cityNode);

  knowledgeRelations.push({
    id: uuid(),
    sourceNodeId: cityNode.id,
    relationshipTypeId: relationshipTypes.cityInState.id,
    targetNodeId: stateNodeIdByName[state]
  });
}

// Sanity checks -- fail loudly rather than write a broken fixture.
const canonicalKey = n => `${n.nodeTypeId}::${n.canonicalName}`;
const seenNames = new Set();
for (const n of knowledgeNodes) {
  const key = canonicalKey(n);
  if (seenNames.has(key)) throw new Error(`Duplicate (nodeType, canonicalName): ${key}`);
  seenNames.add(key);
}
const seenIds = new Set();
for (const n of knowledgeNodes) {
  if (seenIds.has(n.id)) throw new Error(`Duplicate node id: ${n.id}`);
  seenIds.add(n.id);
}

const graph = {
  nodeTypes: Object.values(nodeTypes),
  relationshipTypes: Object.values(relationshipTypes),
  mediaAssets,
  knowledgeNodes,
  knowledgeRelations
};

fs.writeFileSync(path.join(__dirname, 'world-graph.json'), JSON.stringify(graph, null, 2) + '\n');

console.log('nodeTypes:', graph.nodeTypes.length);
console.log('relationshipTypes:', graph.relationshipTypes.length);
console.log('mediaAssets:', graph.mediaAssets.length);
console.log('knowledgeNodes:', graph.knowledgeNodes.length);
console.log('knowledgeRelations:', graph.knowledgeRelations.length);
