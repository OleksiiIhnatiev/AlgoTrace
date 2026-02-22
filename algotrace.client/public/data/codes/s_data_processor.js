const DATA_LIMIT = 1000;

function processIncomingStream(dataArray) {
    if (!Array.isArray(dataArray)) return [];
    
    const validItems = dataArray.filter(item => {
        return item !== null && item !== undefined && item.isActive === true;
    });

    const normalized = validItems.map(item => ({
        id: item.uuid || generateId(),
        score: Math.max(0, item.points || 0),
        timestamp: new Date().getTime()
    }));

    const totalScore = normalized.reduce((sum, curr) => sum + curr.score, 0);
    
    return { items: normalized, total: totalScore };
}

function generateId() {
    return Math.random().toString(36).substr(2, 9);
}