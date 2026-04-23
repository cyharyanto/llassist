// FilterExpression evaluator (spec: Analyze/Screen)
// One type, two call sites: subset resolution and rendering tools.

/**
 * @typedef {Object} FilterExpression
 * @property {string} [field] - dot-path into entity
 * @property {'eq'|'gt'|'lt'|'gte'|'lte'|'in'|'contains'} [op]
 * @property {*} [value]
 * @property {FilterExpression[]} [and]
 * @property {FilterExpression[]} [or]
 * @property {FilterExpression} [not]
 */

function resolve(entity, fieldPath) {
  return fieldPath.split('.').reduce((obj, key) => obj?.[key], entity);
}

export function evaluateFilter(entity, filter) {
  if (!filter) return true;

  // Logical combinators
  if (filter.and) return filter.and.every(f => evaluateFilter(entity, f));
  if (filter.or) return filter.or.some(f => evaluateFilter(entity, f));
  if (filter.not) return !evaluateFilter(entity, filter.not);

  // Leaf comparison
  const val = resolve(entity, filter.field);
  switch (filter.op) {
    case 'eq': return val === filter.value;
    case 'gt': return val > filter.value;
    case 'lt': return val < filter.value;
    case 'gte': return val >= filter.value;
    case 'lte': return val <= filter.value;
    case 'in': return Array.isArray(filter.value) && filter.value.includes(val);
    case 'contains':
      if (typeof val === 'string') return val.includes(filter.value);
      if (Array.isArray(val)) return val.includes(filter.value);
      return false;
    default:
      throw new Error(`Unknown filter op: ${filter.op}`);
  }
}

/**
 * Apply SubsetDefinition to a candidate set.
 * @param {Array} candidates - full set of entities
 * @param {Object} subset - { type: 'all'|'explicit'|'filter', explicitIds?, filter? }
 * @returns {Array} filtered set
 */
export function applySubset(candidates, subset) {
  if (!subset || subset.type === 'all') return candidates;
  if (subset.type === 'explicit') {
    const ids = new Set(subset.explicitIds || []);
    return candidates.filter(c => ids.has(c.id));
  }
  if (subset.type === 'filter') {
    return candidates.filter(c => evaluateFilter(c, subset.filter));
  }
  throw new Error(`Unknown subset type: ${subset.type}`);
}
