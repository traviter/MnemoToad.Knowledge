function(label) {
    var letters = 'abcdefghijklmnopqrstuvwxyz';
    var random = '';
    for (var i = 0; i < 16; i++) {
        random += letters.charAt(Math.floor(Math.random() * letters.length));
    }
    return 'ZZKarate' + (label || '') + random;
}
