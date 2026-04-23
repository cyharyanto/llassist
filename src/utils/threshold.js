// Threshold derivation (spec: Analyze/Screen)
// Reasoning is source; scores are derived.
// Inclusion decisions MUST be recomputed live from scores under the active threshold.
// Zero network calls on threshold change.
// One function, called from every view that needs it.

/**
 * Derive inclusion status for a single RQ assessment.
 * @param {Object} rqAssessment - { rqId, relevanceScore, contributionScore, ... }
 * @param {number} threshold - active threshold (UI slider value)
 * @returns {Object} { isRelevant, isContributing, rqId }
 */
export function deriveRQStatus(rqAssessment, threshold) {
  return {
    rqId: rqAssessment.rqId,
    isRelevant: rqAssessment.relevanceScore >= threshold,
    isContributing: rqAssessment.contributionScore >= threshold,
  };
}

/**
 * Derive full inclusion status for an assessment.
 * @param {Object} assessment - { perRQ: [{ rqId, relevanceScore, contributionScore }] }
 * @param {number} threshold
 * @returns {Object} { mustRead, perRQ: [{ rqId, isRelevant, isContributing }] }
 */
export function deriveAssessmentStatus(assessment, threshold) {
  const perRQ = assessment.perRQ.map(rq => deriveRQStatus(rq, threshold));
  const mustRead = perRQ.some(rq => rq.isRelevant || rq.isContributing);
  return { mustRead, perRQ };
}

/**
 * Derive stats for a set of assessments under a threshold.
 * Used by get-assessment-stats rendering tool and the field-map views.
 * @param {Array} assessments
 * @param {number} threshold
 * @param {string} [rqId] - filter to specific RQ; null = all
 * @returns {Object} { total, relevant, contributing, mustRead, byYear: [{ year, relevant, contributing, total }] }
 */
export function deriveAssessmentStats(assessments, threshold, rqId = null) {
  let relevant = 0;
  let contributing = 0;
  let mustRead = 0;
  const byYear = new Map();

  for (const a of assessments) {
    const status = deriveAssessmentStatus(a, threshold);
    const rqs = rqId
      ? status.perRQ.filter(r => r.rqId === rqId)
      : status.perRQ;

    const isRelevant = rqs.some(r => r.isRelevant);
    const isContributing = rqs.some(r => r.isContributing);

    if (isRelevant) relevant++;
    if (isContributing) contributing++;
    // mustRead scoped to filtered RQs, not global across all RQs
    if (isRelevant || isContributing) mustRead++;

    // Year-based aggregation requires article data — deferred to caller
    // This function operates on assessments only
  }

  return {
    total: assessments.length,
    relevant,
    contributing,
    mustRead,
  };
}

/**
 * Derive per-RQ distribution table (spec: Map the field).
 * Which questions are underserved, contributing-to-relevant ratios.
 * @param {Array} assessments
 * @param {number} threshold
 * @param {Array} researchQuestions - [{ id, text }]
 * @returns {Array} [{ rqId, rqText, relevant, contributing, ratio, total }]
 */
export function deriveRQDistribution(assessments, threshold, researchQuestions) {
  return researchQuestions.map(rq => {
    let relevant = 0;
    let contributing = 0;

    for (const a of assessments) {
      const rqA = a.perRQ.find(r => r.rqId === rq.id);
      if (!rqA) continue;
      const status = deriveRQStatus(rqA, threshold);
      if (status.isRelevant) relevant++;
      if (status.isContributing) contributing++;
    }

    return {
      rqId: rq.id,
      rqText: rq.text,
      relevant,
      contributing,
      ratio: relevant > 0 ? contributing / relevant : 0,
      total: assessments.length,
    };
  });
}
