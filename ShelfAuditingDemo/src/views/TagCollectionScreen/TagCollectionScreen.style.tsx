import { StyleSheet } from 'react-native'

export const styles = StyleSheet.create({
    mainContainer: {
        flex: 1,
        padding: 12,
        backgroundColor: 'black'
    },
    tagBlockStyle: {
        justifyContent: 'center',
        flex: 1,
        alignItems: 'center',
        height: 150,
        maxWidth: 100,
        marginRight: 8,
        marginBottom: 8,
        backgroundColor: '#2B2B2B'
    },
    imageThumbnail: {
        justifyContent: 'center',
        alignItems: 'center',
        height: '100%',
        width: 65
    },
    tagLabel: {
        color: 'white', 
        opacity: 0.8,
        textAlign: 'left',
        fontSize: 12
    },
    tagContainer: {
        flex: 1, 
        justifyContent: 'center',
        padding: 6
    },
    searchInputContainerStyle: {
        backgroundColor: 'rgba(255, 255, 255, 0.2)', 
        borderRadius: 10
    },
    searchContainerStyle: {
        backgroundColor: 'transparent', 
        padding: 0, 
        marginBottom: 12
    },
    imageContainer: {
        alignSelf: 'center', 
        padding: 6
    }
})