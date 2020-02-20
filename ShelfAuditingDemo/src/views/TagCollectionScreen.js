import React, { Component } from 'react';
import { View, Image, Text, TouchableOpacity, FlatList, StyleSheet } from 'react-native';
import { SearchBar } from 'react-native-elements';

class TagCollectionScreen extends React.Component {

    constructor(props) {
        super(props);
        this.state = {
            filter: '',
            allTags: [],
            filterTags: []
        };
    }

    componentDidMount() {
        const { navigation, } = this.props;
        let tags = navigation.getParam('tags', []);
        this.setState({
            allTags: tags, 
            filterTags: tags 
        });
    }

    render() {
        const { filter } = this.state;
        const { 
            mainContainer,
            searchContainerStyle,
            searchInputContainerStyle,
            TagBlockStyle, 
            imageThumbnail,
            tagContainer,
            tagLabel
        } = this.styles;
        
        return (
            <View style={mainContainer}>
                <SearchBar
                    inputContainerStyle={searchInputContainerStyle}
                    containerStyle={searchContainerStyle}
                    placeholder="Search"
                    onChangeText={this.updateSearchFilter}
                    value={filter}
                />
                
                <FlatList 
                    numColumns={4}
                    data={ this.state.filterTags } 
                    renderItem={({item}) =>
                        <TouchableOpacity activeOpacity={0.6} style={TagBlockStyle} 
                            onPress={() => this.selectTag(item)}>
                            <View>
                                <View style={{ alignSelf: 'center', height: '60%', padding: 6 }}>
                                    { item.name.toLocaleLowerCase() != 'product' && item.name.toLocaleLowerCase() != 'gap' &&
                                        <Image style={imageThumbnail} source={{ uri: item.imageUrl }} />
                                    }
                                    { item.name.toLocaleLowerCase() == 'product' &&
                                        <Image style={imageThumbnail} source={require('../assets/product.jpg')} />
                                    }
                                    { item.name.toLocaleLowerCase() == 'gap' &&
                                        <Image style={imageThumbnail} source={require('../assets/gap.jpg')} />
                                    }
                                </View>
                                
                                <View style={[tagContainer, { height: '40%' }]}>
                                    <Text numberOfLines={3} style={tagLabel}>{item.name}</Text>
                                </View>
                            </View>
                        </TouchableOpacity>
                    }
                />

            </View>
        );
    }

    selectTag(tag) {
        const { navigation } = this.props;        
        navigation.state.params.returnData(tag);
        navigation.goBack();
    }

    updateSearchFilter = (filter) => {
        let allTags = this.state.allTags;
        
        if (!filter) {
            this.setState({ filterTags: allTags });
        } else {
            let filterData = [];
            allTags.forEach(t => {
                let tagName = t.name.toLocaleLowerCase();
                if (tagName.includes(filter.toLocaleLowerCase())) {
                    filterData.push(t);
                }
            });
            this.setState({ filterTags: filterData });
        }
        this.setState({ filter });
    }
    
    styles = StyleSheet.create({
        mainContainer: {
            flex: 1,
            padding: 12,
            backgroundColor: 'black'
        },
        TagBlockStyle: {
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
        }
    })
}

export { TagCollectionScreen };