const { test } = require('node:test');
const assert = require('node:assert/strict');
const { isValidRound } = require('./rounds.cjs');
const round = {roundId:'a'.repeat(32), playerId:'pickup-test', level:2, durationMs:30000, itemsSeen:5, itemsCaught:3, score:14};
test('friend mixed gold/red/fuel round and original scoring remain accepted', () => {
  for (const score of [0, 4, 10, 14, 30, 3, 6, 9]) assert.equal(isValidRound({...round,score}), true);
});
test('reject impossible score totals and invalid catch counts', () => {
  for (const change of [{score:31},{score:-1},{score:1.5},{itemsCaught:6},{itemsCaught:0,score:4}])
    assert.equal(isValidRound({...round,...change}), false);
  assert.equal(isValidRound({...round,itemsCaught:0,score:0}), true);
});
