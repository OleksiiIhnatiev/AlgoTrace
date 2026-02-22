export class DataHelper {
    static cleanArray(arr) {
        const valid = arr.filter(item => {
            return item !== null && item !== undefined && item.isActive === true;
        });
        return valid;
    }

    static normalizeStats(validArray) {
        const out = validArray.map(item => ({
            id: item.uuid || this.makeId(),
            score: Math.max(0, item.points || 0),
            timestamp: Date.now()
        }));
        return out;
    }

    static getSum(arr) {
        return arr.reduce((sum, curr) => sum + curr.score, 0);
    }

    static makeId() { 
        return "id_" + Math.random(); 
    }
}